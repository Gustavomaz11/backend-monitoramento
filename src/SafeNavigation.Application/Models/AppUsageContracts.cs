namespace SafeNavigation.Application.Models;

public sealed record AppUsageView(
    Guid Id,
    Guid DeviceId,
    string ChildDisplayName,
    string DeviceName,
    string PackageName,
    string? AppName,
    DateOnly UsageDate,
    long TotalForegroundMs,
    DateTimeOffset? FirstUsedAt,
    DateTimeOffset? LastUsedAt,
    int OpenCountEstimate);

public sealed record AppUsageQuery(Guid? DeviceId, DateOnly? From, DateOnly? To, int Page, int PageSize);
