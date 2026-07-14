using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

public sealed class SyncService(ISafeNavigationDbContext db, IClock clock)
{
    public async Task<SyncBatchResponse> IngestAsync(Guid deviceActorId, SyncBatchRequest request, CancellationToken cancellationToken)
    {
        if (deviceActorId != request.DeviceId) throw new ForbiddenOperationException();

        var device = await db.Devices
            .Include(x => x.Config)
            .Include(x => x.Child)
            .FirstOrDefaultAsync(x => x.Id == request.DeviceId, cancellationToken);
        if (device is null) throw new ResourceNotFoundException("Device not found.");

        var existing = await db.SyncBatches
            .FirstOrDefaultAsync(x => x.DeviceId == request.DeviceId && x.ClientBatchId == request.ClientBatchId, cancellationToken);

        if (existing is not null)
        {
            return new SyncBatchResponse(existing.Id, existing.Status, existing.RecordsAccepted, clock.UtcNow, device.Config?.ConfigVersion ?? 1);
        }

        var batch = new SyncBatch
        {
            DeviceId = request.DeviceId,
            ClientBatchId = request.ClientBatchId,
            StartedAt = clock.UtcNow,
            Status = "accepted"
        };

        var accepted = 0;
        accepted += AddAppUsages(request.DeviceId, request.AppUsages);
        accepted += AddDomainAccesses(device, request.DomainAccesses);
        accepted += AddBlockAttempts(device, request.BlockAttempts);

        batch.RecordsReceived = accepted;
        batch.RecordsAccepted = accepted;
        batch.CompletedAt = clock.UtcNow;
        device.LastSyncAt = clock.UtcNow;

        db.SyncBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        return new SyncBatchResponse(batch.Id, batch.Status, batch.RecordsAccepted, clock.UtcNow, device.Config?.ConfigVersion ?? 1);
    }

    private int AddAppUsages(Guid deviceId, IReadOnlyList<AppUsageRecord>? appUsages)
    {
        if (appUsages is null) return 0;

        foreach (var item in appUsages)
        {
            db.AppUsages.Add(new AppUsage
            {
                Id = item.LocalId,
                DeviceId = deviceId,
                PackageName = item.PackageName,
                AppName = item.AppName,
                UsageDate = item.UsageDate,
                TotalForegroundMs = item.TotalForegroundMs,
                FirstUsedAt = item.FirstUsedAt,
                LastUsedAt = item.LastUsedAt,
                OpenCountEstimate = item.OpenCountEstimate,
                CreatedAt = clock.UtcNow
            });
        }

        return appUsages.Count;
    }

    private int AddDomainAccesses(SafeNavigation.Domain.Entities.Device device, IReadOnlyList<DomainAccessRecord>? domainAccesses)
    {
        if (domainAccesses is null) return 0;

        foreach (var item in domainAccesses)
        {
            db.DomainAccesses.Add(new DomainAccess
            {
                Id = item.LocalId,
                DeviceId = device.Id,
                Domain = item.Domain?.Trim().ToLowerInvariant(),
                IpAddress = item.IpAddress,
                Protocol = item.Protocol,
                Port = item.Port,
                FirstAccessAt = item.FirstAccessAt,
                LastAccessAt = item.LastAccessAt,
                AccessCount = item.AccessCount,
                ForegroundPackageName = item.ForegroundPackageName,
                CorrelationType = item.CorrelationType,
                Source = item.Source,
                CreatedAt = clock.UtcNow
            });

            AddSensitiveCategoryAlert(device, item);
        }

        return domainAccesses.Count;
    }

    private int AddBlockAttempts(SafeNavigation.Domain.Entities.Device device, IReadOnlyList<BlockAttemptRecord>? blockAttempts)
    {
        if (blockAttempts is null) return 0;

        foreach (var item in blockAttempts)
        {
            db.BlockAttempts.Add(new BlockAttempt
            {
                Id = item.LocalId,
                DeviceId = device.Id,
                BlockingRuleId = item.RuleId,
                Domain = item.Domain?.Trim().ToLowerInvariant(),
                IpAddress = item.IpAddress,
                Protocol = item.Protocol,
                Port = item.Port,
                AttemptedAt = item.AttemptedAt,
                ForegroundPackageName = item.ForegroundPackageName,
                CorrelationType = item.CorrelationType
            });

            AddBlockAttemptAlert(device, item);
        }

        return blockAttempts.Count;
    }

    private void AddSensitiveCategoryAlert(SafeNavigation.Domain.Entities.Device device, DomainAccessRecord item)
    {
        var alertType = item.Category switch
        {
            "adult" => "adult",
            "gambling" => "gambling",
            "violence" => "violence",
            "malicious" => "malicious",
            _ => null
        };

        if (alertType is null || device.Child is null) return;

        db.Alerts.Add(new Alert
        {
            GuardianId = device.Child.GuardianId,
            ChildId = device.ChildId,
            DeviceId = device.Id,
            AlertType = alertType,
            Severity = "critical",
            Title = "Categoria sensivel detectada",
            Summary = $"Acesso classificado como {item.Category}. Apenas metadados foram registrados.",
            RelatedEntityType = "domain_access",
            RelatedEntityId = item.LocalId,
            CreatedAt = clock.UtcNow
        });
    }

    private void AddBlockAttemptAlert(SafeNavigation.Domain.Entities.Device device, BlockAttemptRecord item)
    {
        if (device.Child is null) return;

        db.Alerts.Add(new Alert
        {
            GuardianId = device.Child.GuardianId,
            ChildId = device.ChildId,
            DeviceId = device.Id,
            AlertType = "manual_block",
            Severity = "warning",
            Title = "Tentativa bloqueada",
            Summary = "Uma regra familiar bloqueou uma tentativa de acesso.",
            RelatedEntityType = "block_attempt",
            RelatedEntityId = item.LocalId,
            CreatedAt = clock.UtcNow
        });
    }
}
