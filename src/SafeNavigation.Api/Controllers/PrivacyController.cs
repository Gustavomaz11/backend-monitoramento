using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/privacy")]
[Authorize(Policy = "GuardianOnly")]
public sealed class PrivacyController(PrivacyService privacyService) : ControllerBase
{
    [HttpPost("export")]
    public async Task<ActionResult<PrivacyExportResponse>> Export(CancellationToken cancellationToken)
    {
        return Accepted(await privacyService.RequestExportAsync(this.ActorId(), cancellationToken));
    }

    [HttpPost("delete-all")]
    public async Task<ActionResult<PrivacyDeleteAllResponse>> DeleteAll(CancellationToken cancellationToken)
    {
        return Accepted(await privacyService.RequestDeleteAllAsync(this.ActorId(), cancellationToken));
    }
}
