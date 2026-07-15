using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class DashboardService(ISafeNavigationDbContext db, IClock clock)
{
    private static readonly TimeZoneInfo BrasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<DashboardSummaryView> GetSummaryAsync(Guid guardianId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, BrasiliaTimeZone).DateTime);
        var sevenDaysAgo = today.AddDays(-6);
        var since = clock.UtcNow.AddDays(-1);

        var devices = await db.Devices
            .Where(x => x.Child!.GuardianId == guardianId)
            .ToListAsync(cancellationToken);
        var deviceIds = devices.Select(x => x.Id).ToList();
        var lastSyncAt = devices.MaxBy(x => x.LastSyncAt ?? x.CreatedAt)?.LastSyncAt;
        var deviceStatus = devices.Count == 0 ? "sem dispositivos" : string.Join(", ", devices.Select(x => x.Status).Distinct());

        if (deviceIds.Count == 0)
        {
            return new DashboardSummaryView(
                ScreenTimeTodayMs: 0,
                TopApps: [],
                TopDomains: [],
                Categories: [],
                DailyPoints: EmptyDailyPoints(sevenDaysAgo),
                BlockedAttemptsCount: 0,
                DeviceStatus: deviceStatus,
                LastSyncAt: lastSyncAt);
        }

        var todayAppUsages = await db.AppUsages
            .Where(x => deviceIds.Contains(x.DeviceId) && x.UsageDate == today)
            .OrderByDescending(x => x.TotalForegroundMs)
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

        var screenTimeTodayMs = todayAppUsages.Sum(x => x.TotalForegroundMs);
        var topApps = todayAppUsages.Take(5).ToList();

        var topDomains = await db.DomainAccesses
            .Where(x => deviceIds.Contains(x.DeviceId))
            .OrderByDescending(x => x.AccessCount)
            .ThenByDescending(x => x.LastAccessAt)
            .Take(5)
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

        var categoryRows = await db.DomainAccesses
            .Where(x => deviceIds.Contains(x.DeviceId))
            .Select(x => new
            {
                CategoryName = x.Category == null ? null : x.Category.Name,
                CategoryDisplayName = x.Category == null ? null : x.Category.DisplayName,
                CategoryRiskLevel = x.Category == null ? null : (int?)x.Category.RiskLevel,
                x.AccessCount
            })
            .ToListAsync(cancellationToken);
        var categories = categoryRows
            .GroupBy(x => new
            {
                Name = x.CategoryName ?? "unknown",
                DisplayName = x.CategoryDisplayName ?? "Sites desconhecidos",
                RiskLevel = x.CategoryRiskLevel ?? 2
            })
            .Select(x => new CategorySummaryView(x.Key.Name, x.Key.DisplayName, x.Sum(y => y.AccessCount), x.Key.RiskLevel))
            .OrderByDescending(x => x.AccessCount)
            .ToList();

        var appUsageByDay = await db.AppUsages
            .Where(x => deviceIds.Contains(x.DeviceId) && x.UsageDate >= sevenDaysAgo && x.UsageDate <= today)
            .GroupBy(x => x.UsageDate)
            .Select(x => new { Date = x.Key, TotalMs = x.Sum(y => y.TotalForegroundMs) })
            .ToListAsync(cancellationToken);

        var blockAttempts = await db.BlockAttempts
            .Where(x => deviceIds.Contains(x.DeviceId) && x.AttemptedAt >= clock.UtcNow.AddDays(-7))
            .ToListAsync(cancellationToken);
        var blockAttemptsByDay = blockAttempts
            .GroupBy(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.AttemptedAt, BrasiliaTimeZone).DateTime))
            .Select(x => new { Date = x.Key, Count = x.Count() })
            .ToList();

        var dailyPoints = Enumerable.Range(0, 7)
            .Select(offset => sevenDaysAgo.AddDays(offset))
            .Select(date =>
            {
                var totalMs = appUsageByDay.FirstOrDefault(x => x.Date == date)?.TotalMs ?? 0;
                var blocked = blockAttemptsByDay.FirstOrDefault(x => x.Date == date)?.Count ?? 0;
                return new DailyPointView(date, Math.Round(totalMs / 3_600_000d, 2), blocked);
            })
            .ToList();

        var blockedAttemptsCount = await db.BlockAttempts
            .CountAsync(x => deviceIds.Contains(x.DeviceId) && x.AttemptedAt >= since, cancellationToken);

        return new DashboardSummaryView(
            screenTimeTodayMs,
            topApps,
            topDomains,
            categories,
            dailyPoints,
            blockedAttemptsCount,
            deviceStatus,
            lastSyncAt);
    }

    private static IReadOnlyList<DailyPointView> EmptyDailyPoints(DateOnly firstDate) =>
        Enumerable.Range(0, 7)
            .Select(offset => new DailyPointView(firstDate.AddDays(offset), 0, 0))
            .ToList();
}
