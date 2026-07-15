using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

public sealed class PairingService(
    ISafeNavigationDbContext db,
    ITokenService tokenService,
    IClock clock,
    IOptions<PairingOptions> options)
{
    public async Task<PairingCodeResponse> CreateCodeAsync(
        Guid guardianId,
        CreatePairingCodeRequest request,
        CancellationToken cancellationToken)
    {
        var guardianExists = await db.Guardians.AnyAsync(x => x.Id == guardianId, cancellationToken);
        if (!guardianExists) throw new ResourceNotFoundException("Guardian not found.");

        var code = CreatePairingCode();
        var expiresAt = clock.UtcNow.AddMinutes(options.Value.CodeLifetimeMinutes);
        db.PairingCodes.Add(new PairingCode
        {
            GuardianId = guardianId,
            CodeHash = tokenService.HashOpaqueToken(code),
            ChildDisplayName = request.ChildDisplayName.Trim(),
            DeviceName = request.DeviceName?.Trim(),
            ExpiresAt = expiresAt,
            CreatedAt = clock.UtcNow
        });

        db.AuditLogs.Add(new AuditLog
        {
            ActorType = "guardian",
            ActorId = guardianId,
            Action = "pairing_code.created",
            EntityType = "pairing_code",
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return new PairingCodeResponse(code, expiresAt, $"navegacao-segura://pair?code={code}");
    }

    public async Task<DeviceAuthResponse> CompleteAsync(CompletePairingRequest request, CancellationToken cancellationToken)
    {
        var codeHash = tokenService.HashOpaqueToken(request.PairingCode.Trim());
        var pairingCode = await db.PairingCodes
            .Include(x => x.Guardian)
            .FirstOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken);

        if (pairingCode is null) throw new ResourceNotFoundException("Pairing code not found.");
        if (pairingCode.UsedAt is not null || pairingCode.ExpiresAt <= clock.UtcNow)
        {
            throw new GoneException("Pairing code expired or already used.");
        }

        var child = new Child
        {
            GuardianId = pairingCode.GuardianId,
            DisplayName = pairingCode.ChildDisplayName,
            CreatedAt = clock.UtcNow
        };

        var device = new Device
        {
            Child = child,
            DevicePublicId = $"dev_{Guid.NewGuid():N}",
            Name = pairingCode.DeviceName ?? $"{pairingCode.ChildDisplayName} Android",
            AppVersion = request.DeviceInfo.AppVersion,
            AndroidVersion = request.DeviceInfo.AndroidVersion,
            Manufacturer = request.DeviceInfo.Manufacturer,
            Model = request.DeviceInfo.Model,
            CreatedAt = clock.UtcNow
        };

        var config = new DeviceConfig { Device = device, UpdatedAt = clock.UtcNow };
        pairingCode.UsedAt = clock.UtcNow;

        db.Children.Add(child);
        db.Devices.Add(device);
        db.DeviceConfigs.Add(config);

        var tokenPair = tokenService.CreateDeviceTokens(device);
        db.DeviceRefreshTokens.Add(new DeviceRefreshToken
        {
            Device = device,
            TokenHash = tokenService.HashOpaqueToken(tokenPair.RefreshToken),
            ExpiresAt = clock.UtcNow.AddDays(options.Value.DeviceRefreshTokenDays),
            CreatedAt = clock.UtcNow
        });

        db.AuditLogs.Add(new AuditLog
        {
            ActorType = "device",
            ActorId = device.Id,
            Action = "device.paired",
            EntityType = "device",
            EntityId = device.Id,
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return new DeviceAuthResponse(device.Id, tokenPair.AccessToken, tokenPair.RefreshToken, ToDto(config));
    }

    public async Task<DeviceAuthResponse> RefreshDeviceAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashOpaqueToken(request.RefreshToken);
        var storedToken = await db.DeviceRefreshTokens
            .Include(x => x.Device)
            .ThenInclude(x => x!.Config)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null ||
            storedToken.RevokedAt is not null ||
            storedToken.ExpiresAt <= clock.UtcNow ||
            storedToken.Device?.Status != "active")
        {
            throw new UnauthorizedOperationException("Invalid device refresh token.");
        }

        var device = storedToken.Device ?? throw new UnauthorizedOperationException("Invalid device refresh token.");
        var config = device.Config ?? throw new ResourceNotFoundException("Device configuration not found.");
        storedToken.RevokedAt = clock.UtcNow;

        var tokenPair = tokenService.CreateDeviceTokens(device);
        db.DeviceRefreshTokens.Add(new DeviceRefreshToken
        {
            DeviceId = device.Id,
            TokenHash = tokenService.HashOpaqueToken(tokenPair.RefreshToken),
            ExpiresAt = clock.UtcNow.AddDays(options.Value.DeviceRefreshTokenDays),
            CreatedAt = clock.UtcNow
        });
        db.AuditLogs.Add(new AuditLog
        {
            ActorType = "device",
            ActorId = device.Id,
            Action = "device.token_refreshed",
            EntityType = "device",
            EntityId = device.Id,
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return new DeviceAuthResponse(device.Id, tokenPair.AccessToken, tokenPair.RefreshToken, ToDto(config));
    }

    private static DeviceConfigDto ToDto(DeviceConfig config) =>
        new(config.RetentionDays, config.VpnEnabled, config.UsageStatsEnabled, config.SyncIntervalMinutes, config.Timezone, config.ConfigVersion);

    private static string CreatePairingCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 100_000_000);
        return value.ToString("D8");
    }
}

public sealed class PairingOptions
{
    public int CodeLifetimeMinutes { get; set; } = 10;
    public int DeviceRefreshTokenDays { get; set; } = 90;
}
