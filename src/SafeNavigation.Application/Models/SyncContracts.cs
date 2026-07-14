namespace SafeNavigation.Application.Models;

public sealed record SyncBatchRequest(
    Guid ClientBatchId,
    Guid DeviceId,
    DateTimeOffset OccurredFrom,
    DateTimeOffset OccurredTo,
    IReadOnlyList<AppUsageRecord>? AppUsages,
    IReadOnlyList<DomainAccessRecord>? DomainAccesses,
    IReadOnlyList<BlockAttemptRecord>? BlockAttempts);

public sealed record SyncBatchResponse(Guid SyncBatchId, string Status, int RecordsAccepted, DateTimeOffset ServerTime, long ConfigVersion);

public sealed record AppUsageRecord(
    Guid LocalId,
    string PackageName,
    string? AppName,
    DateOnly UsageDate,
    long TotalForegroundMs,
    DateTimeOffset? FirstUsedAt,
    DateTimeOffset? LastUsedAt,
    int OpenCountEstimate);

public sealed record DomainAccessRecord(
    Guid LocalId,
    string? Domain,
    string? IpAddress,
    string Protocol,
    int? Port,
    string? Category,
    DateTimeOffset FirstAccessAt,
    DateTimeOffset LastAccessAt,
    int AccessCount,
    string? ForegroundPackageName,
    string CorrelationType,
    string Source);

public sealed record BlockAttemptRecord(
    Guid LocalId,
    string? Domain,
    string? IpAddress,
    string Protocol,
    int? Port,
    DateTimeOffset AttemptedAt,
    Guid? RuleId,
    string? ForegroundPackageName,
    string CorrelationType,
    string Source);
