using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/app-usages")]
public sealed class AppUsagesController(AppUsageService appUsageService) : ControllerBase
{
    [Authorize(Policy = "GuardianOnly")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUsageView>>> List(
        [FromQuery] Guid? deviceId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = limit <= 0 ? 200 : limit;
        return Ok(await appUsageService.ListGuardianAppUsagesAsync(this.ActorId(), deviceId, effectiveLimit, cancellationToken));
    }
}
