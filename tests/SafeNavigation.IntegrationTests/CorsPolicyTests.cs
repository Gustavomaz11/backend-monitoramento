using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace SafeNavigation.IntegrationTests;

public sealed class CorsPolicyTests
{
    private const string FrontendOrigin = "https://frontend-monitoramento.vercel.app";

    [Fact]
    public async Task SignalRPreflightAllowsCredentialsForConfiguredFrontend()
    {
        await using var baseFactory = new SafeNavigationFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", FrontendOrigin));
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/hubs/live-stream/negotiate?negotiateVersion=1");
        request.Headers.Add("Origin", FrontendOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add(
            "Access-Control-Request-Headers",
            "authorization,x-requested-with,x-signalr-user-agent");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(FrontendOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }
}
