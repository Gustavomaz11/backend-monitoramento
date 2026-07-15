using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SafeNavigation.Application.Models;

namespace SafeNavigation.IntegrationTests;

public sealed class PrivacyDeletionTests
{
    [Fact]
    public async Task DeletionAnonymizesAccountAndInvalidatesAccessImmediately()
    {
        await using var factory = new SafeNavigationFactory();
        using var client = factory.CreateClient();
        const string email = "delete@example.com";
        const string password = "very-secure-password";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterGuardianRequest(email, password, "Responsavel", true));
        registerResponse.EnsureSuccessStatusCode();
        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var deleteResponse = await client.PostAsync("/api/v1/privacy/delete-all", null);
        Assert.Equal(HttpStatusCode.Accepted, deleteResponse.StatusCode);

        var rejectedDashboard = await client.GetAsync("/api/v1/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedDashboard.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var rejectedLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedLogin.StatusCode);

        var newRegistration = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterGuardianRequest(email, password, "Nova conta", true));
        Assert.Equal(HttpStatusCode.Created, newRegistration.StatusCode);
    }
}
