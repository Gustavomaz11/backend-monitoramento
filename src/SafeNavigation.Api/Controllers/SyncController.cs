using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeNavigation.Api.Validation;
using SafeNavigation.Application.Models;
using SafeNavigation.Application.Services;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/sync")]
public sealed class SyncController(SyncService syncService) : ControllerBase
{
    [Authorize(Policy = "DeviceOnly")]
    [HttpPost("batches")]
    public async Task<ActionResult<SyncBatchResponse>> CreateBatch(
        SyncBatchRequest request,
        IValidator<SyncBatchRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        var response = await syncService.IngestAsync(this.ActorId(), request, cancellationToken);
        return Accepted(response);
    }
}
