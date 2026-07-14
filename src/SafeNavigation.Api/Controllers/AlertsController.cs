using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Api.Validation;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize(Policy = "GuardianOnly")]
public sealed class AlertsController(AlertsService alertsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        return Ok(await alertsService.ListAsync(this.ActorId(), status, cancellationToken));
    }

    [HttpPatch("{alertId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid alertId,
        UpdateAlertStatusRequest request,
        IValidator<UpdateAlertStatusRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        await alertsService.UpdateStatusAsync(this.ActorId(), alertId, request, cancellationToken);
        return NoContent();
    }
}
