namespace SafeNavigation.Domain.Entities;

public sealed class SyncBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }
    public Guid ClientBatchId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Status { get; set; } = "accepted";
    public int RecordsReceived { get; set; }
    public int RecordsAccepted { get; set; }
    public string? ErrorSummary { get; set; }
}
