using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [Authorize(Policy = "GuardianOnly")]
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryView>> Summary(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetSummaryAsync(this.ActorId(), cancellationToken));
    }
}
