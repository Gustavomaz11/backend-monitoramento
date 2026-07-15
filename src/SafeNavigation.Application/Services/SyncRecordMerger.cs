using SafeNavigation.Application.Models;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

internal static class SyncRecordMerger
{
    public static void Merge(AppUsage target, AppUsageRecord source)
    {
        target.AppName = source.AppName ?? target.AppName;
        target.TotalForegroundMs = Math.Max(target.TotalForegroundMs, source.TotalForegroundMs);
        target.FirstUsedAt = Min(target.FirstUsedAt, source.FirstUsedAt);
        target.LastUsedAt = Max(target.LastUsedAt, source.LastUsedAt);
        target.OpenCountEstimate = Math.Max(target.OpenCountEstimate, source.OpenCountEstimate);
    }

    public static void Merge(DomainAccess target, DomainAccessRecord source, Guid categoryId)
    {
        target.Domain = NormalizeDomain(source.Domain) ?? target.Domain;
        target.IpAddress = source.IpAddress ?? target.IpAddress;
        target.Protocol = source.Protocol;
        target.Port = source.Port ?? target.Port;
        target.CategoryId = categoryId;
        target.FirstAccessAt = target.FirstAccessAt <= source.FirstAccessAt ? target.FirstAccessAt : source.FirstAccessAt;
        target.LastAccessAt = target.LastAccessAt >= source.LastAccessAt ? target.LastAccessAt : source.LastAccessAt;
        target.AccessCount = Math.Max(target.AccessCount, source.AccessCount);
        target.ForegroundPackageName = source.ForegroundPackageName ?? target.ForegroundPackageName;
        target.CorrelationType = source.CorrelationType;
        target.Source = source.Source;
    }

    public static string? NormalizeDomain(string? domain) =>
        string.IsNullOrWhiteSpace(domain) ? null : domain.Trim().TrimEnd('.').ToLowerInvariant();

    private static DateTimeOffset? Min(DateTimeOffset? left, DateTimeOffset? right) =>
        left is null ? right : right is null || left <= right ? left : right;

    private static DateTimeOffset? Max(DateTimeOffset? left, DateTimeOffset? right) =>
        left is null ? right : right is null || left >= right ? left : right;
}
