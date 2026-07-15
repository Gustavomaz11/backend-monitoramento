using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class DomainAccessService(ISafeNavigationDbContext db)
{
    public async Task<PagedResponse<DomainAccessView>> ListGuardianDomainAccessesAsync(
        Guid guardianId,
        DomainAccessQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.DomainAccesses.Where(x =>
            x.Device!.Child!.GuardianId == guardianId &&
            x.Source == "browser_navigation");

        if (request.DeviceId is not null) query = query.Where(x => x.DeviceId == request.DeviceId);
        if (!string.IsNullOrWhiteSpace(request.Domain))
        {
            var domain = request.Domain.Trim().ToLowerInvariant();
            query = query.Where(x => x.Domain != null && x.Domain.Contains(domain));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim().ToLowerInvariant();
            query = query.Where(x => x.Category != null && x.Category.Name == category);
        }

        if (request.From is not null) query = query.Where(x => x.LastAccessAt >= request.From);
        if (request.To is not null) query = query.Where(x => x.LastAccessAt <= request.To);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.LastAccessAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResponse<DomainAccessView>(items, page, pageSize, totalCount);
    }
}
