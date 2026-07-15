using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SafeNavigation.Application.Models;
using SafeNavigation.Infrastructure.Persistence;

namespace SafeNavigation.IntegrationTests;

public sealed class DeviceRemovalTests
{
    [Fact]
    public async Task GuardianCanRevokeOwnedDeviceAndItsTokens()
    {
        await using var factory = new SafeNavigationFactory();
        using var client = factory.CreateClient();
        var guardian = await RegisterGuardianAsync(client, "device-removal@example.com");
        var device = await PairDeviceAsync(client, guardian.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardian.AccessToken);
        var removeResponse = await client.DeleteAsync($"/api/v1/devices/{device.DeviceId}");

        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
        var devices = await client.GetFromJsonAsync<List<DeviceSummary>>("/api/v1/devices");
        Assert.Empty(devices!);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", device.AccessToken);
        var configResponse = await client.GetAsync($"/api/v1/devices/{device.DeviceId}/config");
        Assert.Equal(HttpStatusCode.Unauthorized, configResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeNavigationDbContext>();
        Assert.All(db.DeviceRefreshTokens.Where(x => x.DeviceId == device.DeviceId), token => Assert.NotNull(token.RevokedAt));
        Assert.Contains(db.AuditLogs, log => log.Action == "device.revoked" && log.EntityId == device.DeviceId);
    }

    [Fact]
    public async Task GuardianCannotRevokeAnotherGuardiansDevice()
    {
        await using var factory = new SafeNavigationFactory();
        using var client = factory.CreateClient();
        var owner = await RegisterGuardianAsync(client, "device-owner@example.com");
        var device = await PairDeviceAsync(client, owner.AccessToken);
        var otherGuardian = await RegisterGuardianAsync(client, "other-guardian@example.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherGuardian.AccessToken);
        var removeResponse = await client.DeleteAsync($"/api/v1/devices/{device.DeviceId}");

        Assert.Equal(HttpStatusCode.NotFound, removeResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeNavigationDbContext>();
        Assert.Equal("active", db.Devices.Single(x => x.Id == device.DeviceId).Status);
    }

    private static async Task<AuthResponse> RegisterGuardianAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterGuardianRequest(email, "very-secure-password", "Responsavel", true));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task<DeviceAuthResponse> PairDeviceAsync(HttpClient client, string guardianAccessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardianAccessToken);
        var pairingResponse = await client.PostAsJsonAsync(
            "/api/v1/pairing-codes",
            new CreatePairingCodeRequest("Crianca", "Celular Android"));
        pairingResponse.EnsureSuccessStatusCode();
        var pairing = (await pairingResponse.Content.ReadFromJsonAsync<PairingCodeResponse>())!;

        client.DefaultRequestHeaders.Authorization = null;
        var completeResponse = await client.PostAsJsonAsync(
            "/api/v1/device-pairing/complete",
            new CompletePairingRequest(pairing.PairingCode, new DeviceInfo("1.0", "14", "Google", "Pixel")));
        completeResponse.EnsureSuccessStatusCode();
        return (await completeResponse.Content.ReadFromJsonAsync<DeviceAuthResponse>())!;
    }
}
