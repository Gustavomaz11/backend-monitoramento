namespace SafeNavigation.Domain.Entities;

public sealed class PrivacyRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuardianId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = "accepted";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
