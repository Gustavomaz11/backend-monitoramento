using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Api.Validation;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterGuardianRequest request,
        IValidator<RegisterGuardianRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        var response = await authService.RegisterAsync(request, cancellationToken);
        return Created("/api/v1/auth/register", response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Ok(await authService.RefreshAsync(request, cancellationToken));
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        await authService.LogoutAsync(request.RefreshToken, this.ActorId(), cancellationToken);
        return NoContent();
    }
}
