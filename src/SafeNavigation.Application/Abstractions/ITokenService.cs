using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Abstractions;

public interface ITokenService
{
    TokenPair CreateGuardianTokens(Guardian guardian);
    TokenPair CreateDeviceTokens(Device device);
    string HashOpaqueToken(string token);
}

public sealed record TokenPair(string AccessToken, string RefreshToken, int ExpiresIn);
