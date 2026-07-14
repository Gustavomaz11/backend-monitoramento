namespace SafeNavigation.Application.Models;

public sealed record PrivacyExportResponse(
    Guid RequestId,
    string Status,
    DateTimeOffset CreatedAt,
    int ChildrenCount,
    int DevicesCount,
    int RulesCount,
    int AlertsCount,
    int AppUsageRecords,
    int DomainAccessRecords,
    int BlockAttemptRecords);

public sealed record PrivacyDeleteAllResponse(Guid RequestId, string Status, DateTimeOffset CreatedAt);
