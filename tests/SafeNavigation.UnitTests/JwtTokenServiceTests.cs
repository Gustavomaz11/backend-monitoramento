using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Domain.Entities;
using SafeNavigation.Infrastructure.Security;

namespace SafeNavigation.UnitTests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateTokens_UsesDifferentActorTypesAndAudiences()
    {
        var service = new JwtTokenService(
            Options.Create(new JwtOptions
            {
                SigningKey = "unit-test-signing-key-with-at-least-32-chars",
                GuardianAudience = "guardian-audience",
                DeviceAudience = "device-audience"
            }),
            new FixedClock());

        var guardianTokens = service.CreateGuardianTokens(new Guardian { Id = Guid.NewGuid(), Email = "parent@example.com" });
        var device = new Device { Id = Guid.NewGuid(), DevicePublicId = "device" };
        var deviceTokens = service.CreateDeviceTokens(device);

        var handler = new JwtSecurityTokenHandler();
        var guardianJwt = handler.ReadJwtToken(guardianTokens.AccessToken);
        var deviceJwt = handler.ReadJwtToken(deviceTokens.AccessToken);

        Assert.Contains("guardian-audience", guardianJwt.Audiences);
        Assert.Contains("device-audience", deviceJwt.Audiences);
        Assert.Equal("guardian", guardianJwt.Claims.First(x => x.Type == "actor_type").Value);
        Assert.Equal("device", deviceJwt.Claims.First(x => x.Type == "actor_type").Value);
        Assert.False(string.IsNullOrWhiteSpace(guardianJwt.Id));
        Assert.False(string.IsNullOrWhiteSpace(deviceJwt.Id));
        Assert.NotEqual(
            deviceTokens.AccessToken,
            service.CreateDeviceTokens(device).AccessToken);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
    }
}
