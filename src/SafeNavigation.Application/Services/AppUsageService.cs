using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class AppUsageService(ISafeNavigationDbContext db)
{
    public async Task<PagedResponse<AppUsageView>> ListGuardianAppUsagesAsync(
        Guid guardianId,
        AppUsageQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.AppUsages.Where(x => x.Device!.Child!.GuardianId == guardianId);

        if (request.DeviceId is not null) query = query.Where(x => x.DeviceId == request.DeviceId);
        if (request.From is not null) query = query.Where(x => x.UsageDate >= request.From);
        if (request.To is not null) query = query.Where(x => x.UsageDate <= request.To);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.UsageDate)
            .ThenByDescending(x => x.TotalForegroundMs)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResponse<AppUsageView>(items, page, pageSize, totalCount);
    }
}
