using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class DeviceService(ISafeNavigationDbContext db, IClock clock)
{
    public async Task<IReadOnlyList<DeviceSummary>> ListGuardianDevicesAsync(Guid guardianId, CancellationToken cancellationToken)
    {
        return await db.Devices
            .Where(x => x.Child!.GuardianId == guardianId)
            .OrderByDescending(x => x.LastSyncAt ?? x.CreatedAt)
            .Select(x => new DeviceSummary(x.Id, x.Child!.DisplayName, x.Name, x.Status, x.LastSyncAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceConfigDto> GetConfigAsync(Guid deviceId, Guid actorId, string actorType, CancellationToken cancellationToken)
    {
        await EnsureDeviceAccessAsync(deviceId, actorId, actorType, cancellationToken);
        var config = await db.DeviceConfigs.FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
        if (config is null) throw new ResourceNotFoundException("Device config not found.");
        return ToDto(config);
    }

    public async Task<DeviceConfigDto> UpdateConfigAsync(Guid deviceId, Guid guardianId, DeviceConfigDto request, CancellationToken cancellationToken)
    {
        await EnsureDeviceAccessAsync(deviceId, guardianId, "guardian", cancellationToken);
        var config = await db.DeviceConfigs.FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
        if (config is null) throw new ResourceNotFoundException("Device config not found.");

        config.RetentionDays = request.RetentionDays;
        config.VpnEnabled = request.VpnEnabled;
        config.UsageStatsEnabled = request.UsageStatsEnabled;
        config.SyncIntervalMinutes = request.SyncIntervalMinutes;
        config.Timezone = request.Timezone;
        config.ConfigVersion += 1;
        config.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(config);
    }

    private async Task EnsureDeviceAccessAsync(Guid deviceId, Guid actorId, string actorType, CancellationToken cancellationToken)
    {
        if (actorType == "device" && deviceId == actorId) return;

        if (actorType == "guardian")
        {
            var hasAccess = await db.Devices.AnyAsync(x => x.Id == deviceId && x.Child!.GuardianId == actorId, cancellationToken);
            if (hasAccess) return;
        }

        throw new ForbiddenOperationException();
    }

    private static DeviceConfigDto ToDto(SafeNavigation.Domain.Entities.DeviceConfig config) =>
        new(config.RetentionDays, config.VpnEnabled, config.UsageStatsEnabled, config.SyncIntervalMinutes, config.Timezone, config.ConfigVersion);
}
