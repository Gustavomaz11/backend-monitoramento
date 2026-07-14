using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class DomainAccessService(ISafeNavigationDbContext db)
{
    public async Task<IReadOnlyList<DomainAccessView>> ListGuardianDomainAccessesAsync(
        Guid guardianId,
        Guid? deviceId,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 500);
        var query = db.DomainAccesses
            .Where(x => x.Device!.Child!.GuardianId == guardianId);

        if (deviceId is not null)
        {
            query = query.Where(x => x.DeviceId == deviceId);
        }

        return await query
            .OrderByDescending(x => x.LastAccessAt)
            .Take(normalizedLimit)
            .Select(x => new DomainAccessView(
                x.Id,
                x.DeviceId,
                x.Device!.Child!.DisplayName,
                x.Device.Name,
                x.Domain,
                x.IpAddress,
                x.Protocol,
                x.Port,
                x.Category == null ? null : x.Category.Name,
                x.FirstAccessAt,
                x.LastAccessAt,
                x.AccessCount,
                x.ForegroundPackageName,
                x.CorrelationType,
                x.Source))
            .ToListAsync(cancellationToken);
    }
}
