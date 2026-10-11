using System.Text.Json;
using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Resonance.Extensions;
using Resonance.Http.Filters;
using Resonance.Models;
using Resonance.Services;

namespace Resonance.Http.Modules;

public sealed class ProfileModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        var profiles = endpoints.MapProductionApi().AddEndpointFilter(EndpointFilters.NoStoreAsync);

        profiles.MapPost("/getProfile", GetAsync);
        profiles.MapPost("/getUserScores", GetScoresAsync);
    }

    private static async Task<ProfileRequest?> ReadRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<ProfileRequest>(
                request.Body, JsonSerializerOptions.Web, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<Results<Ok<ProfileResponse>, BadRequest<ProfileResponse>, NotFound<ProfileResponse>>> GetAsync(
        HttpRequest request, IProfileService profiles, CancellationToken cancellationToken)
    {
        var profileRequest = await ReadRequestAsync(request, cancellationToken);

        if (profileRequest?.Id is not > 0)
            return TypedResults.BadRequest(new ProfileResponse("Invalid profile request."));

        var profile = await profiles.GetAsync(profileRequest.Id.Value, cancellationToken);

        return profile is null
            ? TypedResults.NotFound(new ProfileResponse("User not found."))
            : TypedResults.Ok(new ProfileResponse(User: profile));
    }

    private static async Task<Results<Ok<UserScoresResponse>, BadRequest<UserScoresResponse>, NotFound<UserScoresResponse>>> GetScoresAsync(
        HttpRequest request, IProfileService profiles, CancellationToken cancellationToken)
    {
        var profileRequest = await ReadRequestAsync(request, cancellationToken);

        if (profileRequest?.Id is not > 0)
            return TypedResults.BadRequest(new UserScoresResponse("Invalid scores request."));

        return await profiles.ExistsAsync(profileRequest.Id.Value, cancellationToken)
            ? TypedResults.Ok(new UserScoresResponse())
            : TypedResults.NotFound(new UserScoresResponse("User not found."));
    }
}
