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
