using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SafeNavigation.Api.Middleware;
using SafeNavigation.Application;
using SafeNavigation.Infrastructure;
using SafeNavigation.Infrastructure.Persistence;
using SafeNavigation.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (builder.Environment.IsDevelopment() && allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        allowedOrigins = allowedOrigins.Where(origin => origin != "*").ToArray();
        if (!builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
        {
            var productionOrigin = builder.Configuration["Cors:ProductionOrigin"];
            if (!string.IsNullOrWhiteSpace(productionOrigin)) allowedOrigins = [productionOrigin];
        }

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production.");
        }

        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 characters.");
}
if (!builder.Environment.IsDevelopment() && jwtOptions.SigningKey.StartsWith("dev-only", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Jwt:SigningKey must be replaced in production.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudiences = [jwtOptions.GuardianAudience, jwtOptions.DeviceAudience],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                var actorType = context.Principal?.FindFirst("actor_type")?.Value;
                if (!Guid.TryParse(subject, out var actorId))
                {
                    context.Fail("Invalid token subject.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<SafeNavigationDbContext>();
                var active = actorType switch
                {
                    "guardian" => await db.Guardians.AnyAsync(x => x.Id == actorId && x.Status == "active"),
                    "device" => await db.Devices.AnyAsync(x => x.Id == actorId && x.Status == "active"),
                    _ => false
                };
                if (!active) context.Fail("Token actor is no longer active.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GuardianOnly", policy => policy.RequireRole("guardian").RequireClaim("actor_type", "guardian"));
    options.AddPolicy("DeviceOnly", policy => policy.RequireRole("device").RequireClaim("actor_type", "device"));
    options.AddPolicy("AuthenticatedActor", policy => policy.RequireAuthenticatedUser());
});
builder.Services.AddHealthChecks().AddDbContextCheck<SafeNavigationDbContext>("postgres");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value ?? "authenticated"
            : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var forwardedHeaders = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    forwardedHeaders.KnownNetworks.Clear();
    forwardedHeaders.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeaders);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SafeNavigationDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<ApplicationExceptionMiddleware>();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapHealthChecks("/health", new HealthCheckOptions()).AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
