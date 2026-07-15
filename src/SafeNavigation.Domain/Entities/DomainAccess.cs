namespace SafeNavigation.Domain.Entities;

public sealed class DomainAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid ClientRecordId { get; set; }
    public Device? Device { get; set; }
    public string? Domain { get; set; }
    public string? IpAddress { get; set; }
    public string Protocol { get; set; } = "unknown";
    public int? Port { get; set; }
    public Guid? CategoryId { get; set; }
    public DomainCategory? Category { get; set; }
    public DateTimeOffset FirstAccessAt { get; set; }
    public DateTimeOffset LastAccessAt { get; set; }
    public int AccessCount { get; set; }
    public string? ForegroundPackageName { get; set; }
    public string CorrelationType { get; set; } = "none";
    public string Source { get; set; } = "unknown";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
