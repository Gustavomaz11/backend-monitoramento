using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Infrastructure.Persistence;
using SafeNavigation.Infrastructure.Security;

namespace SafeNavigation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var connectionString = DatabaseConnectionStringFactory.Create(
                configuration["DATABASE_URL"],
                configuration["Database:SslMode"])
            ?? configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        services.AddDbContext<SafeNavigationDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        services.AddScoped<ISafeNavigationDbContext>(provider => provider.GetRequiredService<SafeNavigationDbContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
