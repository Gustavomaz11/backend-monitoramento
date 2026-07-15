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
    public async Task<ActionResult<PagedResponse<AppUsageView>>> List(
        [FromQuery] Guid? deviceId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new AppUsageQuery(deviceId, from, to, page, pageSize);
        return Ok(await appUsageService.ListGuardianAppUsagesAsync(this.ActorId(), query, cancellationToken));
    }
}
