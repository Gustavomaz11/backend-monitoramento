using System.Collections.Concurrent;

namespace SafeNavigation.Api.LiveStreaming;

public sealed record LiveStreamSession(
    Guid SessionId,
    Guid GuardianId,
    Guid DeviceId,
    string GuardianConnectionId,
    string? DeviceConnectionId);

public sealed class LiveStreamSessionRegistry
{
    private readonly ConcurrentDictionary<Guid, LiveStreamSession> _sessions = new();
    private readonly object _startGate = new();

    public bool TryStart(Guid sessionId, Guid guardianId, Guid deviceId, string guardianConnectionId)
    {
        lock (_startGate)
        {
            if (_sessions.Values.Any(x =>
                    x.GuardianConnectionId == guardianConnectionId || x.DeviceId == deviceId)) return false;
            return _sessions.TryAdd(
                sessionId,
                new LiveStreamSession(sessionId, guardianId, deviceId, guardianConnectionId, null));
        }
    }

    public bool TryGet(Guid sessionId, out LiveStreamSession session) =>
        _sessions.TryGetValue(sessionId, out session!);

    public bool TryClaimDevice(Guid sessionId, Guid deviceId, string connectionId, out LiveStreamSession session)
    {
        session = null!;
        while (_sessions.TryGetValue(sessionId, out var current))
        {
            if (current.DeviceId != deviceId) return false;
            if (current.DeviceConnectionId is not null && current.DeviceConnectionId != connectionId) return false;

            var updated = current with { DeviceConnectionId = connectionId };
            if (_sessions.TryUpdate(sessionId, updated, current))
            {
                session = updated;
                return true;
            }
        }

        return false;
    }

    public bool TryRemove(Guid sessionId, out LiveStreamSession session) =>
        _sessions.TryRemove(sessionId, out session!);

    public IReadOnlyList<LiveStreamSession> RemoveByConnection(string connectionId)
    {
        var removed = new List<LiveStreamSession>();
        foreach (var session in _sessions.Values.Where(x =>
                     x.GuardianConnectionId == connectionId || x.DeviceConnectionId == connectionId))
        {
            if (_sessions.TryRemove(session.SessionId, out var value)) removed.Add(value);
        }

        return removed;
    }
}
