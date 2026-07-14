namespace SafeNavigation.Domain.Entities;

public sealed class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuardianId { get; set; }
    public Guid ChildId { get; set; }
    public Guid DeviceId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string Status { get; set; } = "new";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
