namespace SafeNavigation.Domain.Entities;

public sealed class DeviceConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }
    public int RetentionDays { get; set; } = 90;
    public bool VpnEnabled { get; set; } = true;
    public bool UsageStatsEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 60;
    public string Timezone { get; set; } = "America/Sao_Paulo";
    public string UsageScheduleJson { get; set; } = "[]";
    public long ConfigVersion { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
