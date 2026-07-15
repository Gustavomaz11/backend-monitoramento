using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Api.Validation;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
public sealed class DevicesController(DeviceService deviceService) : ControllerBase
{
    [Authorize(Policy = "GuardianOnly")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceSummary>>> List(CancellationToken cancellationToken)
    {
        return Ok(await deviceService.ListGuardianDevicesAsync(this.ActorId(), cancellationToken));
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> Revoke(Guid deviceId, CancellationToken cancellationToken)
    {
        await deviceService.RevokeAsync(deviceId, this.ActorId(), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "AuthenticatedActor")]
    [HttpGet("{deviceId:guid}/config")]
    public async Task<ActionResult<DeviceConfigDto>> GetConfig(Guid deviceId, CancellationToken cancellationToken)
    {
        return Ok(await deviceService.GetConfigAsync(deviceId, this.ActorId(), this.ActorType(), cancellationToken));
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpPut("{deviceId:guid}/config")]
    public async Task<ActionResult<DeviceConfigDto>> UpdateConfig(
        Guid deviceId,
        DeviceConfigDto request,
        IValidator<DeviceConfigDto> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Ok(await deviceService.UpdateConfigAsync(deviceId, this.ActorId(), request, cancellationToken));
    }
}
