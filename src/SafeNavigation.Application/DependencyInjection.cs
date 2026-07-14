using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.Configure<PairingOptions>(configuration.GetSection("Pairing"));
        services.AddScoped<AuthService>();
        services.AddScoped<PairingService>();
        services.AddScoped<DeviceService>();
        services.AddScoped<DomainAccessService>();
        services.AddScoped<AppUsageService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<SyncService>();
        services.AddScoped<RulesService>();
        services.AddScoped<AlertsService>();
        services.AddScoped<PrivacyService>();
        return services;
    }
}
