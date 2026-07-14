using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafeNavigation.Application.Models;
using SafeNavigation.Infrastructure.Persistence;

namespace SafeNavigation.IntegrationTests;

public sealed class BackendFlowTests
{
    [Fact]
    public async Task GuardianCanPairDeviceAndDeviceCanSyncIdempotently()
    {
        await using var factory = new SafeNavigationFactory();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterGuardianRequest("parent@example.com", "very-secure-password", "Responsavel", true));
        registerResponse.EnsureSuccessStatusCode();
        var guardianAuth = await ReadJsonAsync<AuthResponse>(registerResponse);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardianAuth.AccessToken);
        var pairingResponse = await client.PostAsJsonAsync(
            "/api/v1/pairing-codes",
            new CreatePairingCodeRequest("Crianca", "Celular Android"));
        await EnsureSuccessWithBodyAsync(pairingResponse);
        var pairing = await ReadJsonAsync<PairingCodeResponse>(pairingResponse);

        client.DefaultRequestHeaders.Authorization = null;
        var completeResponse = await client.PostAsJsonAsync(
            "/api/v1/device-pairing/complete",
            new CompletePairingRequest(pairing.PairingCode, new DeviceInfo("1.0", "14", "Google", "Pixel")));
        completeResponse.EnsureSuccessStatusCode();
        var deviceAuth = await ReadJsonAsync<DeviceAuthResponse>(completeResponse);

        var batchId = Guid.NewGuid();
        var syncRequest = new SyncBatchRequest(
            batchId,
            deviceAuth.DeviceId,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow,
            [new AppUsageRecord(Guid.NewGuid(), "com.example.app", "Example", DateOnly.FromDateTime(DateTime.UtcNow), 1000, null, null, 1)],
            [new DomainAccessRecord(Guid.NewGuid(), "gambling.example", "1.1.1.1", "udp", 53, "gambling", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow, 1, null, "none", "dns")],
            [new BlockAttemptRecord(Guid.NewGuid(), "blocked.example", "1.1.1.1", "udp", 53, DateTimeOffset.UtcNow, null, null, "none", "dns")]);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deviceAuth.AccessToken);
        var syncResponse = await client.PostAsJsonAsync("/api/v1/sync/batches", syncRequest);
        Assert.Equal(HttpStatusCode.Accepted, syncResponse.StatusCode);
        var accepted = await ReadJsonAsync<SyncBatchResponse>(syncResponse);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/sync/batches", syncRequest);
        Assert.Equal(HttpStatusCode.Accepted, duplicateResponse.StatusCode);
        var duplicate = await ReadJsonAsync<SyncBatchResponse>(duplicateResponse);

        Assert.Equal(accepted.SyncBatchId, duplicate.SyncBatchId);
        Assert.Equal(3, accepted.RecordsAccepted);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardianAuth.AccessToken);
        var ruleResponse = await client.PostAsJsonAsync(
            "/api/v1/rules",
            new CreateRuleRequest("domain", "blocked.example", "block", null, null));
        ruleResponse.EnsureSuccessStatusCode();
        var rule = await ReadJsonAsync<BlockingRuleDto>(ruleResponse);
        Assert.Equal("blocked.example", rule.Value);

        var alerts = await ReadJsonAsync<List<AlertDto>>(await client.GetAsync("/api/v1/alerts"));
        Assert.True(alerts.Count >= 2);

        var domainAccesses = await ReadJsonAsync<List<DomainAccessView>>(await client.GetAsync("/api/v1/domain-accesses"));
        Assert.Contains(domainAccesses, access =>
            access.ChildDisplayName == "Crianca" &&
            access.Domain == "gambling.example" &&
            access.LastAccessAt.Offset == TimeSpan.Zero);

        var alertStatusResponse = await client.PatchAsJsonAsync(
            $"/api/v1/alerts/{alerts[0].Id}/status",
            new UpdateAlertStatusRequest("resolved"));
        Assert.Equal(HttpStatusCode.NoContent, alertStatusResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deviceAuth.AccessToken);
        var unblockResponse = await client.PostAsJsonAsync(
            "/api/v1/unblock-requests",
            new CreateUnblockRequest("blocked.example", "Preciso acessar para uma tarefa."));
        unblockResponse.EnsureSuccessStatusCode();
        var unblockRequest = await ReadJsonAsync<UnblockRequestDto>(unblockResponse);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardianAuth.AccessToken);
        var decisionResponse = await client.PostAsJsonAsync(
            $"/api/v1/unblock-requests/{unblockRequest.Id}/decision",
            new UnblockDecisionRequest("approved", "Liberado para estudo."));
        decisionResponse.EnsureSuccessStatusCode();

        var exportResponse = await client.PostAsync("/api/v1/privacy/export", null);
        Assert.Equal(HttpStatusCode.Accepted, exportResponse.StatusCode);
        var export = await ReadJsonAsync<PrivacyExportResponse>(exportResponse);
        Assert.True(export.DevicesCount >= 1);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return value ?? throw new InvalidOperationException("Empty JSON response.");
    }

    private static async Task EnsureSuccessWithBodyAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode}: {body}");
    }
}

public sealed class SafeNavigationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "safe-navigation-tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-with-at-least-32-chars");
        builder.UseSetting("ConnectionStrings:Postgres", "Host=unused;Database=unused;Username=unused;Password=unused");

        builder.ConfigureServices(services =>
        {
            var dbOptions = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<SafeNavigationDbContext>));
            if (dbOptions is not null)
            {
                services.Remove(dbOptions);
            }

            services.AddDbContext<SafeNavigationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
