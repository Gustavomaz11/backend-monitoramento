using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class AlertsService(ISafeNavigationDbContext db, IClock clock)
{
    public async Task<IReadOnlyList<AlertDto>> ListAsync(
        Guid guardianId,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = db.Alerts.Where(x => x.GuardianId == guardianId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AlertDto(x.Id, x.AlertType, x.Severity, x.Title, x.Summary, x.Status, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid guardianId,
        Guid alertId,
        UpdateAlertStatusRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await db.Alerts.FirstOrDefaultAsync(x => x.Id == alertId && x.GuardianId == guardianId, cancellationToken);
        if (alert is null) throw new ResourceNotFoundException("Alert not found.");

        alert.Status = request.Status;
        db.AuditLogs.Add(new SafeNavigation.Domain.Entities.AuditLog
        {
            ActorType = "guardian",
            ActorId = guardianId,
            Action = "alert.status_updated",
            EntityType = "alert",
            EntityId = alert.Id,
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
