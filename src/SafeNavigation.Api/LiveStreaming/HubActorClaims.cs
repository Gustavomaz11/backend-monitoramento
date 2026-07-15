using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SafeNavigation.Api.LiveStreaming;

internal static class HubActorClaims
{
    public static Guid ActorId(this HubCallerContext context)
    {
        var subject = context.User?.FindFirstValue("sub");
        if (Guid.TryParse(subject, out var actorId)) return actorId;
        throw new HubException("Unauthorized actor.");
    }

    public static string ActorType(this HubCallerContext context) =>
        context.User?.FindFirstValue("actor_type") ?? "";
}
