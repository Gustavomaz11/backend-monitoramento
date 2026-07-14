namespace SafeNavigation.Application.Models;

public sealed record DashboardSummaryView(
    long ScreenTimeTodayMs,
    IReadOnlyList<AppUsageView> TopApps,
    IReadOnlyList<DomainAccessView> TopDomains,
    IReadOnlyList<CategorySummaryView> Categories,
    IReadOnlyList<DailyPointView> DailyPoints,
    int BlockedAttemptsCount,
    string DeviceStatus,
    DateTimeOffset? LastSyncAt);

public sealed record CategorySummaryView(string Name, string DisplayName, int AccessCount, int RiskLevel);

public sealed record DailyPointView(DateOnly Date, double ScreenTimeHours, int BlockedAttempts);
