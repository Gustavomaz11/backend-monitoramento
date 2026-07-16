using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SafeNavigation.Api.LiveStreaming;

[Authorize(Policy = "DeviceOnly")]
public sealed class DeviceControlHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(Context.ActorId()));
        await base.OnConnectedAsync();
    }

    public static string DeviceGroup(Guid deviceId) => $"device-control:{deviceId:N}";
}
