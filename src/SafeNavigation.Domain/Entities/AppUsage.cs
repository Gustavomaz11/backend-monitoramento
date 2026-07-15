namespace SafeNavigation.Domain.Entities;

public sealed class AppUsage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid ClientRecordId { get; set; }
    public Device? Device { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string? AppName { get; set; }
    public DateOnly UsageDate { get; set; }
    public long TotalForegroundMs { get; set; }
    public DateTimeOffset? FirstUsedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public int OpenCountEstimate { get; set; }
    public string Source { get; set; } = "usage_stats";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
