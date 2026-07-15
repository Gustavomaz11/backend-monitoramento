namespace SafeNavigation.Application.Models;

public sealed record RegisterGuardianRequest(string Email, string Password, string DisplayName, bool AcceptedTerms);
public sealed record LoginRequest(string Email, string Password);
public sealed record GuardianCredentialVerificationRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);
