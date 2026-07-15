using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SafeNavigation.Application.Models;

namespace SafeNavigation.IntegrationTests;

public sealed class SyncRecordIsolationTests
{
    [Fact]
    public async Task TwoDevicesCanUseTheSameClientRecordIdsWithoutCollisions()
    {
        await using var factory = new SafeNavigationFactory();
        using var client = factory.CreateClient();
        var guardian = await RegisterGuardianAsync(client);
        var firstDevice = await PairDeviceAsync(client, guardian.AccessToken, "Filho 1");
        var secondDevice = await PairDeviceAsync(client, guardian.AccessToken, "Filho 2");
        var appLocalId = Guid.NewGuid();
        var domainLocalId = Guid.NewGuid();

        await SyncAsync(client, firstDevice, appLocalId, domainLocalId);
        await SyncAsync(client, secondDevice, appLocalId, domainLocalId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardian.AccessToken);
        var domains = await client.GetFromJsonAsync<PagedResponse<DomainAccessView>>("/api/v1/domain-accesses?pageSize=100");
        var usages = await client.GetFromJsonAsync<PagedResponse<AppUsageView>>("/api/v1/app-usages?pageSize=100");

        Assert.NotNull(domains);
        Assert.NotNull(usages);
        Assert.Equal(2, domains.TotalCount);
        Assert.Equal(2, domains.Items.Select(x => x.DeviceId).Distinct().Count());
        Assert.Equal(2, usages.TotalCount);
        Assert.Equal(2, usages.Items.Select(x => x.DeviceId).Distinct().Count());
    }

    private static async Task<AuthResponse> RegisterGuardianAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterGuardianRequest("isolation@example.com", "very-secure-password", "Responsavel", true));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task<DeviceAuthResponse> PairDeviceAsync(HttpClient client, string guardianToken, string childName)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardianToken);
        var pairingResponse = await client.PostAsJsonAsync(
            "/api/v1/pairing-codes",
            new CreatePairingCodeRequest(childName, $"Celular {childName}"));
        pairingResponse.EnsureSuccessStatusCode();
        var pairing = (await pairingResponse.Content.ReadFromJsonAsync<PairingCodeResponse>())!;

        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync(
            "/api/v1/device-pairing/complete",
            new CompletePairingRequest(pairing.PairingCode, new DeviceInfo("1.0", "14", "Google", "Pixel")));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceAuthResponse>())!;
    }

    private static async Task SyncAsync(HttpClient client, DeviceAuthResponse device, Guid appLocalId, Guid domainLocalId)
    {
        var now = DateTimeOffset.UtcNow;
        var request = new SyncBatchRequest(
            Guid.NewGuid(),
            device.DeviceId,
            now.AddMinutes(-1),
            now,
            [new AppUsageRecord(appLocalId, "com.example.same", "Same", DateOnly.FromDateTime(DateTime.UtcNow), 1000, now.AddMinutes(-1), now, 1)],
            [new DomainAccessRecord(domainLocalId, "same.example", "1.1.1.1", "tcp", 443, "unknown", now.AddMinutes(-1), now, 1, null, "none", "dns")],
            []);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", device.AccessToken);
        var response = await client.PostAsJsonAsync("/api/v1/sync/batches", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
