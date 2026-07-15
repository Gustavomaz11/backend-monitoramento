using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SafeNavigation.Application.Models;

namespace SafeNavigation.IntegrationTests;

public sealed class DeviceGuardianAccessTests
{
    [Fact]
    public async Task PairedDeviceAcceptsOnlyItsGuardianCredentials()
    {
        await using var factory = new SafeNavigationFactory();
        using var client = factory.CreateClient();

        const string ownerEmail = "owner@example.com";
        const string ownerPassword = "very-secure-password";
        var ownerAuth = await Register(client, ownerEmail, ownerPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAuth.AccessToken);
        var pairingResponse = await client.PostAsJsonAsync(
            "/api/v1/pairing-codes",
            new CreatePairingCodeRequest("Crianca", "Celular Android"));
        pairingResponse.EnsureSuccessStatusCode();
        var pairing = await pairingResponse.Content.ReadFromJsonAsync<PairingCodeResponse>();

        client.DefaultRequestHeaders.Authorization = null;
        var completeResponse = await client.PostAsJsonAsync(
            "/api/v1/device-pairing/complete",
            new CompletePairingRequest(
                pairing!.PairingCode,
                new DeviceInfo("1.0", "14", "Google", "Pixel")));
        completeResponse.EnsureSuccessStatusCode();
        var deviceAuth = await completeResponse.Content.ReadFromJsonAsync<DeviceAuthResponse>();

        await Register(client, "other@example.com", "another-secure-password");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deviceAuth!.AccessToken);

        var accepted = await client.PostAsJsonAsync(
            "/api/v1/device-auth/verify-guardian",
            new GuardianCredentialVerificationRequest(ownerEmail, ownerPassword));
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/v1/device-auth/verify-guardian",
            new GuardianCredentialVerificationRequest(ownerEmail, "wrong-password"));
        Assert.Equal(HttpStatusCode.Forbidden, wrongPassword.StatusCode);

        var otherGuardian = await client.PostAsJsonAsync(
            "/api/v1/device-auth/verify-guardian",
            new GuardianCredentialVerificationRequest("other@example.com", "another-secure-password"));
        Assert.Equal(HttpStatusCode.Forbidden, otherGuardian.StatusCode);
    }

    private static async Task<AuthResponse> Register(HttpClient client, string email, string password)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterGuardianRequest(email, password, "Responsavel", true));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
