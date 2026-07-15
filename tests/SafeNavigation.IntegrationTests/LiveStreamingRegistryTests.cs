using SafeNavigation.Api.LiveStreaming;

namespace SafeNavigation.IntegrationTests;

public sealed class LiveStreamingRegistryTests
{
    [Fact]
    public void PresenceRemainsOnlineUntilLastDeviceConnectionCloses()
    {
        var registry = new DevicePresenceRegistry();
        var deviceId = Guid.NewGuid();

        Assert.True(registry.Add(deviceId, "connection-1"));
        Assert.False(registry.Add(deviceId, "connection-2"));
        Assert.True(registry.IsOnline(deviceId));
        Assert.False(registry.Remove(deviceId, "connection-1"));
        Assert.True(registry.IsOnline(deviceId));
        Assert.True(registry.Remove(deviceId, "connection-2"));
        Assert.False(registry.IsOnline(deviceId));
    }

    [Fact]
    public void SessionCanOnlyBeClaimedByItsDeviceAndOneConnection()
    {
        var registry = new LiveStreamSessionRegistry();
        var sessionId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        Assert.True(registry.TryStart(sessionId, guardianId, deviceId, "guardian-connection"));
        Assert.False(registry.TryStart(Guid.NewGuid(), guardianId, Guid.NewGuid(), "guardian-connection"));
        Assert.False(registry.TryStart(Guid.NewGuid(), Guid.NewGuid(), deviceId, "other-guardian-connection"));
        Assert.False(registry.TryClaimDevice(sessionId, Guid.NewGuid(), "attacker", out _));
        Assert.True(registry.TryClaimDevice(sessionId, deviceId, "device-connection", out var session));
        Assert.Equal("device-connection", session.DeviceConnectionId);
        Assert.False(registry.TryClaimDevice(sessionId, deviceId, "second-device-connection", out _));
    }
}
