namespace SafeNavigation.Domain.Entities;

public sealed class PairingCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuardianId { get; set; }
    public Guardian? Guardian { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string ChildDisplayName { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
