namespace SafeNavigation.Domain.Entities;

public sealed class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Child? Child { get; set; }
    public string DevicePublicId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public string? AppVersion { get; set; }
    public string? AndroidVersion { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string Status { get; set; } = "active";
    public DateTimeOffset? LastSyncAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DeviceConfig? Config { get; set; }
    public List<DeviceRefreshToken> RefreshTokens { get; set; } = [];
}
