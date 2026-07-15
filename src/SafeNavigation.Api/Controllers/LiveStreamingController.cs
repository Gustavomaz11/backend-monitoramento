using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SafeNavigation.Api.LiveStreaming;

namespace SafeNavigation.Api.Controllers;

[ApiController]
[Route("api/v1/live-stream")]
[Authorize(Policy = "AuthenticatedActor")]
public sealed class LiveStreamingController(IOptions<LiveStreamingOptions> options) : ControllerBase
{
    [HttpGet("configuration")]
    public ActionResult<LiveStreamConfiguration> Configuration()
    {
        var iceServers = options.Value.IceServers
            .Where(x => x.Urls.Count > 0)
            .Select(x => new IceServerConfiguration(x.Urls, x.Username, x.Credential))
            .ToList();
        return Ok(new LiveStreamConfiguration(iceServers));
    }
}
