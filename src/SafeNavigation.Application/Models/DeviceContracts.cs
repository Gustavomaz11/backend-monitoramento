namespace SafeNavigation.Application.Models;

public sealed record DeviceSummary(Guid Id, string ChildDisplayName, string Name, string Status, DateTimeOffset? LastSyncAt);
public sealed record DeviceConfigDto(int RetentionDays, bool VpnEnabled, bool UsageStatsEnabled, int SyncIntervalMinutes, string Timezone, long ConfigVersion);
