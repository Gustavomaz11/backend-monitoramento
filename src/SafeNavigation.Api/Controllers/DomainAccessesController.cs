using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/domain-accesses")]
public sealed class DomainAccessesController(DomainAccessService domainAccessService) : ControllerBase
{
    [Authorize(Policy = "GuardianOnly")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DomainAccessView>>> List(
        [FromQuery] Guid? deviceId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = limit <= 0 ? 100 : limit;
        var accesses = await domainAccessService.ListGuardianDomainAccessesAsync(
            this.ActorId(),
            deviceId,
            effectiveLimit,
            cancellationToken);

        return Ok(accesses);
    }
}
