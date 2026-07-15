using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeNavigation.Api.Validation;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/device-auth")]
[Authorize(Policy = "DeviceOnly")]
public sealed class DeviceAuthController(DeviceGuardianAccessService accessService) : ControllerBase
{
    [HttpPost("verify-guardian")]
    [EnableRateLimiting("GuardianCredentialVerification")]
    public async Task<IActionResult> VerifyGuardian(
        GuardianCredentialVerificationRequest request,
        IValidator<GuardianCredentialVerificationRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        await accessService.VerifyAsync(this.ActorId(), request, cancellationToken);
        return NoContent();
    }
}
