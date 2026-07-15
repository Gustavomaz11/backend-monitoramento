using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Models;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

public sealed class PrivacyService(ISafeNavigationDbContext db, IClock clock)
{
    public async Task<PrivacyExportResponse> RequestExportAsync(Guid guardianId, CancellationToken cancellationToken)
    {
        var request = new PrivacyRequest
        {
            GuardianId = guardianId,
            RequestType = "export",
            Status = "accepted",
            CompletedAt = clock.UtcNow,
            CreatedAt = clock.UtcNow
        };

        db.PrivacyRequests.Add(request);
        AddAudit(guardianId, "privacy.export_requested", request.Id);

        var childIds = await db.Children.Where(x => x.GuardianId == guardianId).Select(x => x.Id).ToListAsync(cancellationToken);
        var deviceIds = await db.Devices.Where(x => childIds.Contains(x.ChildId)).Select(x => x.Id).ToListAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new PrivacyExportResponse(
            request.Id,
            request.Status,
            request.CreatedAt,
            childIds.Count,
            deviceIds.Count,
            await db.BlockingRules.CountAsync(x => x.GuardianId == guardianId, cancellationToken),
            await db.Alerts.CountAsync(x => x.GuardianId == guardianId, cancellationToken),
            await db.AppUsages.CountAsync(x => deviceIds.Contains(x.DeviceId), cancellationToken),
            await db.DomainAccesses.CountAsync(x => deviceIds.Contains(x.DeviceId), cancellationToken),
            await db.BlockAttempts.CountAsync(x => deviceIds.Contains(x.DeviceId), cancellationToken));
    }

    public async Task<PrivacyDeleteAllResponse> RequestDeleteAllAsync(Guid guardianId, CancellationToken cancellationToken)
    {
        var request = new PrivacyRequest
        {
            GuardianId = guardianId,
            RequestType = "delete_all",
            Status = "completed",
            CompletedAt = clock.UtcNow,
            CreatedAt = clock.UtcNow
        };
        db.PrivacyRequests.Add(request);
        AddAudit(guardianId, "privacy.delete_all_requested", request.Id);

        var guardian = await db.Guardians.FirstOrDefaultAsync(x => x.Id == guardianId, cancellationToken);
        if (guardian is not null)
        {
            guardian.Email = $"deleted-{guardian.Id:N}@deleted.invalid";
            guardian.DisplayName = "Conta excluida";
            guardian.PasswordHash = "deleted";
            guardian.Status = "deleted";
            guardian.UpdatedAt = clock.UtcNow;
        }

        var childIds = await db.Children.Where(x => x.GuardianId == guardianId).Select(x => x.Id).ToListAsync(cancellationToken);
        var deviceIds = await db.Devices.Where(x => childIds.Contains(x.ChildId)).Select(x => x.Id).ToListAsync(cancellationToken);

        db.DeviceRefreshTokens.RemoveRange(db.DeviceRefreshTokens.Where(x => deviceIds.Contains(x.DeviceId)));
        db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(x => x.GuardianId == guardianId));
        db.AppUsages.RemoveRange(db.AppUsages.Where(x => deviceIds.Contains(x.DeviceId)));
        db.DomainAccesses.RemoveRange(db.DomainAccesses.Where(x => deviceIds.Contains(x.DeviceId)));
        db.BlockAttempts.RemoveRange(db.BlockAttempts.Where(x => deviceIds.Contains(x.DeviceId)));
        db.SyncBatches.RemoveRange(db.SyncBatches.Where(x => deviceIds.Contains(x.DeviceId)));
        db.UnblockRequests.RemoveRange(db.UnblockRequests.Where(x => deviceIds.Contains(x.DeviceId)));
        db.Alerts.RemoveRange(db.Alerts.Where(x => x.GuardianId == guardianId));
        db.BlockingRules.RemoveRange(db.BlockingRules.Where(x => x.GuardianId == guardianId));
        db.DeviceConfigs.RemoveRange(db.DeviceConfigs.Where(x => deviceIds.Contains(x.DeviceId)));
        db.Devices.RemoveRange(db.Devices.Where(x => deviceIds.Contains(x.Id)));
        db.Children.RemoveRange(db.Children.Where(x => childIds.Contains(x.Id)));
        db.PairingCodes.RemoveRange(db.PairingCodes.Where(x => x.GuardianId == guardianId));

        await db.SaveChangesAsync(cancellationToken);
        return new PrivacyDeleteAllResponse(request.Id, request.Status, request.CreatedAt);
    }

    private void AddAudit(Guid guardianId, string action, Guid entityId)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorType = "guardian",
            ActorId = guardianId,
            Action = action,
            EntityType = "privacy_request",
            EntityId = entityId,
            CreatedAt = clock.UtcNow
        });
    }
}
