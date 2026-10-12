using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Resonance.Core.Services;
using Resonance.Extensions;
using Resonance.Http.Filters;
using Resonance.Models;
using Resonance.Server.Extensions;

namespace Resonance.Http.Modules;

public sealed class ProfileModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        var profiles = endpoints.MapProductionApi().AddEndpointFilter(EndpointFilters.NoStoreAsync);

        profiles.MapPost("/getProfile", GetAsync);
        profiles.MapPost("/getUserScores", GetScoresAsync);
    }

    private static async Task<Results<Ok<ProfileResponse>, BadRequest<ProfileResponse>, NotFound<ProfileResponse>>> GetAsync(
        HttpRequest request, IProfileService profiles, TimeProvider clock, IValidator<ProfileRequest> validator, CancellationToken cancellationToken)
    {
        var profileRequest = await GameRequestReader.ReadAsync<ProfileRequest>(request, cancellationToken);

        if (profileRequest is null || !(await validator.ValidateAsync(profileRequest, cancellationToken)).IsValid)
            return TypedResults.BadRequest(new ProfileResponse("Invalid profile request."));

        var profile = await profiles.GetAsync(profileRequest.Id!.Value, cancellationToken);

        return profile is null
            ? TypedResults.NotFound(new ProfileResponse("User not found."))
            : TypedResults.Ok(new ProfileResponse(User: profile.ToProfile(clock.GetUtcNow())));
    }

    private static async Task<Results<Ok<UserScoresResponse>, BadRequest<UserScoresResponse>, NotFound<UserScoresResponse>>> GetScoresAsync(
        HttpRequest request, IProfileService profiles, IValidator<ProfileRequest> validator, CancellationToken cancellationToken)
    {
        var profileRequest = await GameRequestReader.ReadAsync<ProfileRequest>(request, cancellationToken);

        if (profileRequest is null || !(await validator.ValidateAsync(profileRequest, cancellationToken)).IsValid)
            return TypedResults.BadRequest(new UserScoresResponse("Invalid scores request."));

        return await profiles.ExistsAsync(profileRequest.Id!.Value, cancellationToken)
            ? TypedResults.Ok(new UserScoresResponse())
            : TypedResults.NotFound(new UserScoresResponse("User not found."));
    }
}
