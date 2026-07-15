using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Api.Validation;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class PairingController(PairingService pairingService) : ControllerBase
{
    [Authorize(Policy = "GuardianOnly")]
    [HttpPost("pairing-codes")]
    public async Task<ActionResult<PairingCodeResponse>> Create(
        CreatePairingCodeRequest request,
        IValidator<CreatePairingCodeRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        var response = await pairingService.CreateCodeAsync(this.ActorId(), request, cancellationToken);
        return Created("/api/v1/pairing-codes", response);
    }

    [HttpPost("device-pairing/complete")]
    public async Task<ActionResult<DeviceAuthResponse>> Complete(
        CompletePairingRequest request,
        IValidator<CompletePairingRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        var response = await pairingService.CompleteAsync(request, cancellationToken);
        return Created($"/api/v1/devices/{response.DeviceId}", response);
    }

    [HttpPost("device-pairing/refresh")]
    public async Task<ActionResult<DeviceAuthResponse>> RefreshDevice(
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Ok(await pairingService.RefreshDeviceAsync(request, cancellationToken));
    }
}
