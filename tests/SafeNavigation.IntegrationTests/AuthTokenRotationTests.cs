using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SafeNavigation.Application.Models;

namespace SafeNavigation.IntegrationTests;

public sealed class AuthTokenRotationTests
{
    [Fact]
    public async Task RefreshTokenIsRotatedAndRevokedOnLogout()
    {
        await using var factory = new SafeNavigationFactory();
        using var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterGuardianRequest("rotation@example.com", "very-secure-password", "Responsavel", true));
        registerResponse.EnsureSuccessStatusCode();
        var original = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(original.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var rotated = (await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var reusedResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(original.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reusedResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.AccessToken);
        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var revokedResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
    }
}
