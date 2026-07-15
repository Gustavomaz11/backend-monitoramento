using System.Collections.Concurrent;

namespace SafeNavigation.Api.LiveStreaming;

public sealed class DevicePresenceRegistry
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    public bool Add(Guid deviceId, string connectionId)
    {
        var connections = _connections.GetOrAdd(deviceId, _ => []);
        lock (connections)
        {
            var wasOffline = connections.Count == 0;
            connections.Add(connectionId);
            return wasOffline;
        }
    }

    public bool Remove(Guid deviceId, string connectionId)
    {
        if (!_connections.TryGetValue(deviceId, out var connections)) return false;
        lock (connections)
        {
            connections.Remove(connectionId);
            if (connections.Count > 0) return false;
            _connections.TryRemove(new KeyValuePair<Guid, HashSet<string>>(deviceId, connections));
            return true;
        }
    }

    public bool IsOnline(Guid deviceId)
    {
        if (!_connections.TryGetValue(deviceId, out var connections)) return false;
        lock (connections) return connections.Count > 0;
    }
}

