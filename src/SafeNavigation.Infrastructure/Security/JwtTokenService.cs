using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public TokenPair CreateGuardianTokens(Guardian guardian)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, guardian.Id.ToString()),
            new Claim("actor_type", "guardian"),
            new Claim("role", "guardian")
        };

        return new TokenPair(CreateAccessToken(claims, _options.GuardianAudience), CreateOpaqueToken(), _options.AccessTokenMinutes * 60);
    }

    public TokenPair CreateDeviceTokens(Device device)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, device.Id.ToString()),
            new Claim("actor_type", "device"),
            new Claim("role", "device")
        };

        return new TokenPair(CreateAccessToken(claims, _options.DeviceAudience), CreateOpaqueToken(), _options.AccessTokenMinutes * 60);
    }

    public string HashOpaqueToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string CreateAccessToken(IEnumerable<Claim> claims, string audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenClaims = claims.Append(
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: audience,
            claims: tokenClaims,
            notBefore: clock.UtcNow.UtcDateTime,
            expires: clock.UtcNow.AddMinutes(_options.AccessTokenMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
