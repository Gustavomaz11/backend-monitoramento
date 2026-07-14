namespace SafeNavigation.Domain.Entities;

public sealed class BlockingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuardianId { get; set; }
    public Guid? ChildId { get; set; }
    public Guid? DeviceId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Action { get; set; } = "block";
    public int Priority { get; set; } = 100;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
