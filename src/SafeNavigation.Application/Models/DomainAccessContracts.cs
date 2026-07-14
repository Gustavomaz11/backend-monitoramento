namespace SafeNavigation.Application.Models;

public sealed record DomainAccessView(
    Guid Id,
    Guid DeviceId,
    string ChildDisplayName,
    string DeviceName,
    string? Domain,
    string? IpAddress,
    string Protocol,
    int? Port,
    string? Category,
    DateTimeOffset FirstAccessAt,
    DateTimeOffset LastAccessAt,
    int AccessCount,
    string? ForegroundPackageName,
    string CorrelationType,
    string Source);
