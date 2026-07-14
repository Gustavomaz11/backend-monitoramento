namespace SafeNavigation.Domain.Entities;

public sealed class Child
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuardianId { get; set; }
    public Guardian? Guardian { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? BirthYear { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Device> Devices { get; set; } = [];
}
