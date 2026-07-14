using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Application.Errors;

namespace SafeNavigation.Api.Controllers;

internal static class ControllerClaims
{
    public static Guid ActorId(this ControllerBase controller)
    {
        var subject = controller.User.FindFirstValue("sub");
        if (Guid.TryParse(subject, out var actorId)) return actorId;
        throw new UnauthorizedOperationException();
    }

    public static string ActorType(this ControllerBase controller)
    {
        var actorType = controller.User.FindFirstValue("actor_type");
        if (!string.IsNullOrWhiteSpace(actorType)) return actorType;
        throw new UnauthorizedOperationException();
    }
}
