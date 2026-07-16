using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SafeNavigation.Application.Models;
using SafeNavigation.Api.LiveStreaming;
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
        var liveStreamConfiguration = await ReadJsonAsync<LiveStreamConfiguration>(
            await client.GetAsync("/api/v1/live-stream/configuration"));
        Assert.NotEmpty(liveStreamConfiguration.IceServers);

        var emptyDashboard = await ReadJsonAsync<DashboardSummaryView>(await client.GetAsync("/api/v1/dashboard/summary"));
        Assert.Equal(0, emptyDashboard.ScreenTimeTodayMs);
        Assert.Empty(emptyDashboard.TopApps);
        Assert.Empty(emptyDashboard.TopDomains);

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
        Assert.Equal(7, deviceAuth.Config.UsageSchedule.Count);

        var configuredSchedule = deviceAuth.Config with
        {
            UsageSchedule = Enumerable.Range(1, 7)
                .Select(day => new DailyUsageWindowDto(day, day <= 5, 7 * 60, 21 * 60))
                .ToArray()
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardianAuth.AccessToken);
        var updateScheduleResponse = await client.PutAsJsonAsync(
            $"/api/v1/devices/{deviceAuth.DeviceId}/config",
            configuredSchedule);
        updateScheduleResponse.EnsureSuccessStatusCode();
        var updatedConfig = await ReadJsonAsync<DeviceConfigDto>(updateScheduleResponse);
        Assert.Equal(2, updatedConfig.ConfigVersion);
        Assert.False(updatedConfig.UsageSchedule.Single(x => x.DayOfWeek == 6).Enabled);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deviceAuth.AccessToken);
        var deviceConfig = await ReadJsonAsync<DeviceConfigDto>(
            await client.GetAsync($"/api/v1/devices/{deviceAuth.DeviceId}/config"));
        Assert.Equal(updatedConfig.UsageSchedule, deviceConfig.UsageSchedule);

        client.DefaultRequestHeaders.Authorization = null;
        var deviceRefreshResponse = await client.PostAsJsonAsync(
            "/api/v1/device-pairing/refresh",
            new RefreshTokenRequest(deviceAuth.RefreshToken));
        deviceRefreshResponse.EnsureSuccessStatusCode();
        var refreshedDeviceAuth = await ReadJsonAsync<DeviceAuthResponse>(deviceRefreshResponse);
        Assert.Equal(deviceAuth.DeviceId, refreshedDeviceAuth.DeviceId);
        Assert.NotEqual(deviceAuth.AccessToken, refreshedDeviceAuth.AccessToken);
        Assert.NotEqual(deviceAuth.RefreshToken, refreshedDeviceAuth.RefreshToken);

        var reusedDeviceRefreshResponse = await client.PostAsJsonAsync(
            "/api/v1/device-pairing/refresh",
            new RefreshTokenRequest(deviceAuth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reusedDeviceRefreshResponse.StatusCode);
        deviceAuth = refreshedDeviceAuth;

        var batchId = Guid.NewGuid();
        var appUsageLocalId = Guid.NewGuid();
        var domainAccessLocalId = Guid.NewGuid();
        var blockAttemptLocalId = Guid.NewGuid();
        var syncStartedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var syncFinishedAt = DateTimeOffset.UtcNow;
        var syncRequest = new SyncBatchRequest(
            batchId,
            deviceAuth.DeviceId,
            syncStartedAt,
            syncFinishedAt,
            [new AppUsageRecord(appUsageLocalId, "com.example.app", "Example", DateOnly.FromDateTime(DateTime.UtcNow), 1000, null, syncFinishedAt, 1)],
            [
                new DomainAccessRecord(domainAccessLocalId, "gambling.example", null, "https", 443, "gambling", syncFinishedAt.AddMinutes(-1), syncFinishedAt, 1, "com.android.chrome", "observed", "browser_navigation"),
                new DomainAccessRecord(Guid.NewGuid(), "background-api.example", "1.1.1.1", "udp", 53, "unknown", syncFinishedAt, syncFinishedAt, 8, null, "none", "dns")
            ],
            [new BlockAttemptRecord(blockAttemptLocalId, "blocked.example", "1.1.1.1", "udp", 53, syncFinishedAt, null, null, "none", "dns")]);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deviceAuth.AccessToken);
        var syncResponse = await client.PostAsJsonAsync("/api/v1/sync/batches", syncRequest);
        await EnsureSuccessWithBodyAsync(syncResponse);
        Assert.Equal(HttpStatusCode.Accepted, syncResponse.StatusCode);
        var accepted = await ReadJsonAsync<SyncBatchResponse>(syncResponse);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/sync/batches", syncRequest);
        Assert.Equal(HttpStatusCode.Accepted, duplicateResponse.StatusCode);
        var duplicate = await ReadJsonAsync<SyncBatchResponse>(duplicateResponse);

        Assert.Equal(accepted.SyncBatchId, duplicate.SyncBatchId);
        Assert.Equal(3, accepted.RecordsAccepted);

        var updatedSyncRequest = syncRequest with
        {
            ClientBatchId = Guid.NewGuid(),
            OccurredTo = syncFinishedAt.AddMinutes(1),
            AppUsages = [syncRequest.AppUsages![0] with { TotalForegroundMs = 2500, OpenCountEstimate = 2 }],
            DomainAccesses = [syncRequest.DomainAccesses![0] with { LastAccessAt = syncFinishedAt.AddMinutes(1), AccessCount = 4 }]
        };
        var updatedSyncResponse = await client.PostAsJsonAsync("/api/v1/sync/batches", updatedSyncRequest);
        Assert.Equal(HttpStatusCode.Accepted, updatedSyncResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guardianAuth.AccessToken);
        var ruleResponse = await client.PostAsJsonAsync(
            "/api/v1/rules",
            new CreateRuleRequest("domain", "blocked.example", "block", null, null));
        ruleResponse.EnsureSuccessStatusCode();
        var rule = await ReadJsonAsync<BlockingRuleDto>(ruleResponse);
        Assert.Equal("blocked.example", rule.Value);

        var alerts = await ReadJsonAsync<List<AlertDto>>(await client.GetAsync("/api/v1/alerts"));
        Assert.Equal(2, alerts.Count);

        var domainAccesses = await ReadJsonAsync<PagedResponse<DomainAccessView>>(await client.GetAsync("/api/v1/domain-accesses"));
        Assert.Equal(1, domainAccesses.TotalCount);
        var domainAccess = Assert.Single(domainAccesses.Items, access =>
            access.ChildDisplayName == "Crianca" &&
            access.Domain == "gambling.example");
        Assert.Equal(4, domainAccess.AccessCount);
        Assert.Equal(syncFinishedAt.AddMinutes(1), domainAccess.LastAccessAt);
        Assert.Equal("browser_navigation", domainAccess.Source);
        Assert.Equal("https", domainAccess.Protocol);

        var appUsages = await ReadJsonAsync<PagedResponse<AppUsageView>>(await client.GetAsync("/api/v1/app-usages"));
        var appUsage = Assert.Single(appUsages.Items);
        Assert.Equal(2500, appUsage.TotalForegroundMs);
        Assert.Equal(2, appUsage.OpenCountEstimate);

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

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SafeNavigationDbContext>().Database.EnsureCreated();
        return host;
    }
}
