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
    public async Task<ActionResult<PagedResponse<DomainAccessView>>> List(
        [FromQuery] Guid? deviceId,
        [FromQuery] string? domain,
        [FromQuery] string? category,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var accesses = await domainAccessService.ListGuardianDomainAccessesAsync(
            this.ActorId(),
            new DomainAccessQuery(deviceId, domain, category, from, to, page, pageSize),
            cancellationToken);

        return Ok(accesses);
    }
}
