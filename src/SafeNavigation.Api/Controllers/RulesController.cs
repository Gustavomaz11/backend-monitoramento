using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Api.Validation;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class RulesController(RulesService rulesService) : ControllerBase
{
    [Authorize(Policy = "GuardianOnly")]
    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<BlockingRuleDto>>> ListRules(CancellationToken cancellationToken)
    {
        return Ok(await rulesService.ListAsync(this.ActorId(), cancellationToken));
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpPost("rules")]
    public async Task<ActionResult<BlockingRuleDto>> CreateRule(
        CreateRuleRequest request,
        IValidator<CreateRuleRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        var response = await rulesService.CreateAsync(this.ActorId(), request, cancellationToken);
        return Created($"/api/v1/rules/{response.Id}", response);
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpPut("rules/{ruleId:guid}")]
    public async Task<ActionResult<BlockingRuleDto>> UpdateRule(
        Guid ruleId,
        UpdateRuleRequest request,
        IValidator<UpdateRuleRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Ok(await rulesService.UpdateAsync(this.ActorId(), ruleId, request, cancellationToken));
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpDelete("rules/{ruleId:guid}")]
    public async Task<IActionResult> DeleteRule(Guid ruleId, CancellationToken cancellationToken)
    {
        await rulesService.DeleteAsync(this.ActorId(), ruleId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpGet("unblock-requests")]
    public async Task<ActionResult<IReadOnlyList<UnblockRequestDto>>> ListUnblockRequests(CancellationToken cancellationToken)
    {
        return Ok(await rulesService.ListUnblockRequestsAsync(this.ActorId(), cancellationToken));
    }

    [Authorize(Policy = "DeviceOnly")]
    [HttpPost("unblock-requests")]
    public async Task<ActionResult<UnblockRequestDto>> CreateUnblockRequest(
        CreateUnblockRequest request,
        IValidator<CreateUnblockRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        var response = await rulesService.CreateUnblockRequestAsync(this.ActorId(), request, cancellationToken);
        return Created($"/api/v1/unblock-requests/{response.Id}", response);
    }

    [Authorize(Policy = "GuardianOnly")]
    [HttpPost("unblock-requests/{requestId:guid}/decision")]
    public async Task<ActionResult<UnblockRequestDto>> DecideUnblockRequest(
        Guid requestId,
        UnblockDecisionRequest request,
        IValidator<UnblockDecisionRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Ok(await rulesService.DecideUnblockRequestAsync(this.ActorId(), requestId, request, cancellationToken));
    }
}
