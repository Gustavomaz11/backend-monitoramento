namespace SafeNavigation.Domain.Entities;

public sealed class BlockAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }
    public Guid? BlockingRuleId { get; set; }
    public string? Domain { get; set; }
    public string? IpAddress { get; set; }
    public string Protocol { get; set; } = "unknown";
    public int? Port { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public string? ForegroundPackageName { get; set; }
    public string CorrelationType { get; set; } = "none";
    public string ChildRequestStatus { get; set; } = "none";
}
