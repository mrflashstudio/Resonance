using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Resonance.Models;
using Resonance.Routing;
using Resonance.Services;

namespace Resonance.Http.Controllers;

[ApiController]
[Subdomain("production")]
[Route("api")]
[RequestSizeLimit(4096)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileController(IProfileService profiles) : ControllerBase
{
    private async Task<ProfileRequest?> ReadRequestAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<ProfileRequest>(
                Request.Body, JsonSerializerOptions.Web, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [HttpPost("getProfile")]
    public async Task<ActionResult<ProfileResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var request = await ReadRequestAsync(cancellationToken);

        if (request?.Id is not > 0)
            return BadRequest(new ProfileResponse("Invalid profile request."));

        var profile = await profiles.GetAsync(request.Id.Value, cancellationToken);

        return profile is null
            ? NotFound(new ProfileResponse("User not found."))
            : Ok(new ProfileResponse(User: profile));
    }

    [HttpPost("getUserScores")]
    public async Task<ActionResult<UserScoresResponse>> GetScoresAsync(CancellationToken cancellationToken)
    {
        var request = await ReadRequestAsync(cancellationToken);

        if (request?.Id is not > 0)
            return BadRequest(new UserScoresResponse("Invalid scores request."));

        return await profiles.ExistsAsync(request.Id.Value, cancellationToken)
            ? Ok(new UserScoresResponse())
            : NotFound(new UserScoresResponse("User not found."));
    }
}
