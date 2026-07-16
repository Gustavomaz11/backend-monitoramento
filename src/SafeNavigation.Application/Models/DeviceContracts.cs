namespace SafeNavigation.Application.Models;

public sealed record DeviceSummary(Guid Id, string ChildDisplayName, string Name, string Status, DateTimeOffset? LastSyncAt);
public sealed record DailyUsageWindowDto(int DayOfWeek, bool Enabled, int StartMinute, int EndMinute);
public sealed record DeviceConfigDto(
    int RetentionDays,
    bool VpnEnabled,
    bool UsageStatsEnabled,
    int SyncIntervalMinutes,
    string Timezone,
    IReadOnlyList<DailyUsageWindowDto> UsageSchedule,
    long ConfigVersion);
