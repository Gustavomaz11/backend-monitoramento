using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class AppUsageService(ISafeNavigationDbContext db)
{
    public async Task<IReadOnlyList<AppUsageView>> ListGuardianAppUsagesAsync(
        Guid guardianId,
        Guid? deviceId,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 500);
        var query = db.AppUsages
            .Where(x => x.Device!.Child!.GuardianId == guardianId);

        if (deviceId is not null)
        {
            query = query.Where(x => x.DeviceId == deviceId);
        }

        return await query
            .OrderByDescending(x => x.UsageDate)
            .ThenByDescending(x => x.TotalForegroundMs)
            .Take(normalizedLimit)
            .Select(x => new AppUsageView(
                x.Id,
                x.DeviceId,
                x.Device!.Child!.DisplayName,
                x.Device.Name,
                x.PackageName,
                x.AppName,
                x.UsageDate,
                x.TotalForegroundMs,
                x.FirstUsedAt,
                x.LastUsedAt,
                x.OpenCountEstimate))
            .ToListAsync(cancellationToken);
    }
}
