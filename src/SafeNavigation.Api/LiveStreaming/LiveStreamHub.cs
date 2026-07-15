using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;

namespace SafeNavigation.Api.LiveStreaming;

[Authorize(Policy = "AuthenticatedActor")]
public sealed class LiveStreamHub(
    ISafeNavigationDbContext db,
    DevicePresenceRegistry presence,
    LiveStreamSessionRegistry sessions) : Hub
{
    private static readonly HashSet<string> Sources = ["camera_front", "camera_back", "screen"];

    public override async Task OnConnectedAsync()
    {
        if (Context.ActorType() == "device")
        {
            var deviceId = Context.ActorId();
            await Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));
            if (presence.Add(deviceId, Context.ConnectionId))
            {
                var guardianId = await GuardianIdForDevice(deviceId);
                if (guardianId is not null)
                {
                    await Clients.Group(GuardianGroup(guardianId.Value))
                        .SendAsync("DevicePresenceChanged", deviceId.ToString(), true);
                }
            }
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GuardianGroup(Context.ActorId()));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var session in sessions.RemoveByConnection(Context.ConnectionId))
        {
            await NotifyOtherParticipant(session, "StreamEnded", session.SessionId.ToString(), "connection_closed");
        }

        if (Context.ActorType() == "device")
        {
            var deviceId = Context.ActorId();
            if (presence.Remove(deviceId, Context.ConnectionId))
            {
                var guardianId = await GuardianIdForDevice(deviceId);
                if (guardianId is not null)
                {
                    await Clients.Group(GuardianGroup(guardianId.Value))
                        .SendAsync("DevicePresenceChanged", deviceId.ToString(), false);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<bool> GetDevicePresence(Guid deviceId)
    {
        await EnsureGuardianAccess(deviceId);
        return presence.IsOnline(deviceId);
    }

    public async Task RequestStream(Guid deviceId, Guid sessionId, string source, string offerSdp)
    {
        EnsureActorType("guardian");
        await EnsureGuardianAccess(deviceId);
        if (!Sources.Contains(source)) throw new HubException("Invalid stream source.");
        EnsureSdp(offerSdp);
        if (!presence.IsOnline(deviceId)) throw new HubException("Device is offline.");
        if (!sessions.TryStart(sessionId, Context.ActorId(), deviceId, Context.ConnectionId))
        {
            throw new HubException("Stream session already exists.");
        }

        await Clients.Group(DeviceGroup(deviceId))
            .SendAsync("StreamRequested", sessionId.ToString(), source, offerSdp);
    }

    public async Task SubmitAnswer(Guid sessionId, string answerSdp)
    {
        EnsureActorType("device");
        EnsureSdp(answerSdp);
        if (!sessions.TryClaimDevice(sessionId, Context.ActorId(), Context.ConnectionId, out var session))
        {
            throw new HubException("Stream session not found.");
        }

        await Clients.Client(session.GuardianConnectionId)
            .SendAsync("StreamAnswer", sessionId.ToString(), answerSdp);
    }

    public async Task RejectStream(Guid sessionId, string reason)
    {
        EnsureActorType("device");
        if (!sessions.TryClaimDevice(sessionId, Context.ActorId(), Context.ConnectionId, out var session)) return;
        sessions.TryRemove(sessionId, out _);
        await Clients.Client(session.GuardianConnectionId)
            .SendAsync("StreamRejected", sessionId.ToString(), NormalizeReason(reason));
    }

    public async Task RelayIceCandidate(Guid sessionId, string candidate, string? sdpMid, int sdpMLineIndex)
    {
        if (candidate.Length is 0 or > 8_192) throw new HubException("Invalid ICE candidate.");
        if (!sessions.TryGet(sessionId, out var session)) return;

        var actorType = Context.ActorType();
        if (actorType == "guardian" && session.GuardianId == Context.ActorId() && session.GuardianConnectionId == Context.ConnectionId)
        {
            await Clients.Group(DeviceGroup(session.DeviceId))
                .SendAsync("IceCandidate", sessionId.ToString(), candidate, sdpMid, sdpMLineIndex);
            return;
        }

        if (actorType == "device" && session.DeviceId == Context.ActorId())
        {
            if (!sessions.TryClaimDevice(sessionId, Context.ActorId(), Context.ConnectionId, out session)) return;
            await Clients.Client(session.GuardianConnectionId)
                .SendAsync("IceCandidate", sessionId.ToString(), candidate, sdpMid, sdpMLineIndex);
            return;
        }

        throw new HubException("Stream session access denied.");
    }

    public async Task StopStream(Guid sessionId)
    {
        if (!sessions.TryGet(sessionId, out var session)) return;
        EnsureSessionParticipant(session);
        sessions.TryRemove(sessionId, out _);
        await NotifyOtherParticipant(session, "StreamEnded", sessionId.ToString(), "stopped");
    }

    private async Task EnsureGuardianAccess(Guid deviceId)
    {
        EnsureActorType("guardian");
        var guardianId = Context.ActorId();
        var hasAccess = await db.Devices.AnyAsync(x =>
            x.Id == deviceId && x.Status == "active" && x.Child!.GuardianId == guardianId);
        if (!hasAccess) throw new HubException("Device not found.");
    }

    private async Task<Guid?> GuardianIdForDevice(Guid deviceId) =>
        await db.Devices.Where(x => x.Id == deviceId && x.Status == "active")
            .Select(x => (Guid?)x.Child!.GuardianId)
            .FirstOrDefaultAsync();

    private void EnsureSessionParticipant(LiveStreamSession session)
    {
        var actorId = Context.ActorId();
        var allowed = Context.ActorType() switch
        {
            "guardian" => session.GuardianId == actorId && session.GuardianConnectionId == Context.ConnectionId,
            "device" => session.DeviceId == actorId &&
                        (session.DeviceConnectionId is null || session.DeviceConnectionId == Context.ConnectionId),
            _ => false
        };
        if (!allowed) throw new HubException("Stream session access denied.");
    }

    private async Task NotifyOtherParticipant(LiveStreamSession session, string method, params object?[] args)
    {
        if (Context.ConnectionId == session.GuardianConnectionId)
        {
            await Clients.Group(DeviceGroup(session.DeviceId)).SendCoreAsync(method, args);
            return;
        }

        await Clients.Client(session.GuardianConnectionId).SendCoreAsync(method, args);
    }

    private void EnsureActorType(string expected)
    {
        if (Context.ActorType() != expected) throw new HubException("Operation not allowed for this actor.");
    }

    private static void EnsureSdp(string sdp)
    {
        if (string.IsNullOrWhiteSpace(sdp) || sdp.Length > 100_000) throw new HubException("Invalid session description.");
    }

    private static string NormalizeReason(string reason) =>
        string.IsNullOrWhiteSpace(reason) ? "unavailable" : reason.Trim()[..Math.Min(reason.Trim().Length, 200)];

    private static string DeviceGroup(Guid deviceId) => $"live-device:{deviceId:N}";
    private static string GuardianGroup(Guid guardianId) => $"live-guardian:{guardianId:N}";
}
