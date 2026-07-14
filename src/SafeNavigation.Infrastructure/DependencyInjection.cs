using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Infrastructure.Persistence;
using SafeNavigation.Infrastructure.Security;
using System.Net;

namespace SafeNavigation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? ConvertDatabaseUrl(configuration["DATABASE_URL"])
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        services.AddDbContext<SafeNavigationDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        services.AddScoped<ISafeNavigationDbContext>(provider => provider.GetRequiredService<SafeNavigationDbContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    private static string? ConvertDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl)) return null;

        var uri = new Uri(databaseUrl);
        if (uri.Scheme is not ("postgres" or "postgresql"))
        {
            return databaseUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? WebUtility.UrlDecode(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? WebUtility.UrlDecode(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return string.Join(
            ";",
            $"Host={uri.Host}",
            $"Port={port}",
            $"Database={database}",
            $"Username={username}",
            $"Password={password}",
            "SSL Mode=Require",
            "Trust Server Certificate=true");
    }
}
