namespace SafeNavigation.Domain.Entities;

public sealed class UnblockRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string Status { get; set; } = "pending";
    public string? DecisionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DecidedAt { get; set; }
}
