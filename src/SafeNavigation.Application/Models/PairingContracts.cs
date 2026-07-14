namespace SafeNavigation.Application.Models;

public sealed record CreatePairingCodeRequest(string ChildDisplayName, string? DeviceName);
public sealed record PairingCodeResponse(string PairingCode, DateTimeOffset ExpiresAt, string QrPayload);
public sealed record CompletePairingRequest(string PairingCode, DeviceInfo DeviceInfo);
public sealed record DeviceInfo(string? AppVersion, string? AndroidVersion, string? Manufacturer, string? Model);
public sealed record DeviceAuthResponse(Guid DeviceId, string AccessToken, string RefreshToken, DeviceConfigDto Config);
