using System.Text.Json;
using FluentValidation;
using Carter;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Resonance.Configuration;
using Resonance.Core.Authentication;
using Resonance.Core.Configuration;
using Resonance.Core.Models;
using Resonance.Core.Services;
using Resonance.Extensions;
using Resonance.Http.Filters;
using Resonance.Models;

namespace Resonance.Http.Modules;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapProductionApi();

        api.MapPost("/loginAccount", LoginAsync).AddEndpointFilter(EndpointFilters.NoStoreAsync)
            .RequireRateLimiting(RateLimiting.Authentication);
        api.MapPost("/registerAccount", RegisterAsync).RequireRateLimiting(RateLimiting.Authentication);
        api.MapGet("/session", GetAsync).AddEndpointFilter(EndpointFilters.NoStoreAsync);
        api.MapPost("/logoutAccount", LogoutAsync).AddEndpointFilter(EndpointFilters.NoStoreAsync);
    }

    private static async Task<Results<Ok<LoginResponse>, BadRequest<LoginResponse>, JsonHttpResult<LoginResponse>>> LoginAsync(
        HttpContext context, IAuthenticationService authentication, IOptions<ServerOptions> server,
        IOptions<SessionSettings> sessions, IValidator<LoginRequest> validator, CancellationToken cancellationToken)
    {
        var login = await GameRequestReader.ReadAsync<LoginRequest>(context.Request, cancellationToken);

        if (login is null || !(await validator.ValidateAsync(login, cancellationToken)).IsValid)
            return TypedResults.BadRequest(new LoginResponse("Invalid login request."));

        if (login.Client == "web" && !context.Request.HasTrustedOrigin(server.Value))
            return TypedResults.Json(new LoginResponse("Invalid origin."), statusCode: StatusCodes.Status403Forbidden);

        var client = login.Client switch
        {
            "game" => SessionClient.Game,
            "web" => SessionClient.Web,
            _ => (SessionClient?)null
        };

        if (client is null)
            return TypedResults.Ok(new LoginResponse("Unsupported client."));

        var result = await authentication.LoginAsync(login.Login!, login.PasswordProof!, client.Value, cancellationToken);
        var response = new LoginResponse(result.Error, result.Session, result.AccountId);

        if (login.Client == "web" && response.Session is { } token)
        {
            if (context.Request.Cookies.TryGetValue(SessionCookie.Name, out var previous))
                await authentication.RevokeAsync(previous, SessionClient.Web, cancellationToken);

            context.Response.Cookies.Append(SessionCookie.Name, token, SessionCookie.CreateOptions(sessions.Value.Lifetime));
            response = response with { Session = null };
        }

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<RegistrationResponse>, ProblemHttpResult, ValidationProblem>> RegisterAsync(
        HttpRequest request, IAuthenticationService authentication, CancellationToken cancellationToken)
    {
        if (request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>() is { CanHaveBody: false })
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "A registration request body is required.");

        if (!request.HasJsonContentType())
            return TypedResults.Problem(statusCode: StatusCodes.Status415UnsupportedMediaType);

        RegistrationRequest? registration;

        try
        {
            registration = await request.ReadFromJsonAsync<RegistrationRequest>(
                JsonSerializerOptions.Web, cancellationToken);
        }
        catch (JsonException)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "Invalid registration request JSON.");
        }

        if (registration is null)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "A registration request body is required.");

        var result = await authentication.RegisterAsync(registration.Username, registration.Password, cancellationToken);

        if (result.Errors.Count != 0)
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());

        return result.Account is not { } account
            ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Username is already taken.")
            : TypedResults.Created((string?)null, new RegistrationResponse(account.AccountId, account.Username));
    }

    private static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> GetAsync(
        HttpContext context, IAuthenticationService authentication, IProfileService profiles, TimeProvider clock,
        AccountActionLimiter limits,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Cookies.TryGetValue(SessionCookie.Name, out var token) ||
            await authentication.GetSessionAsync(token, SessionClient.Web, cancellationToken) is not { } session)
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);

        if (!limits.TryAcquire(session.UserId, AccountAction.Http, out var retryAfter))
        {
            RateLimiting.SetRejectionHeaders(context, retryAfter);
            return TypedResults.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Too many requests.");
        }

        var account = await profiles.GetAsync(session.UserId, cancellationToken);

        if (account is null)
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);

        context.Response.Cookies.Append(SessionCookie.Name, token, SessionCookie.CreateOptions(session.ExpiresAt - clock.GetUtcNow()));

        return TypedResults.Ok(SessionResponse.FromAccount(account));
    }

    private static async Task<Results<Ok<LogoutResponse>, BadRequest<LogoutResponse>, ProblemHttpResult, JsonHttpResult<LogoutResponse>>> LogoutAsync(
        HttpContext context, IAuthenticationService authentication, IOptions<ServerOptions> server,
        AccountActionLimiter limits, IValidator<LogoutRequest> validator, CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("Origin") || context.Request.Cookies.ContainsKey(SessionCookie.Name))
        {
            if (!context.Request.HasTrustedOrigin(server.Value))
                return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden);

            if (context.Request.Cookies.TryGetValue(SessionCookie.Name, out var cookie))
            {
                var session = await authentication.GetSessionForLogoutAsync(cookie, SessionClient.Web, cancellationToken);

                if (session is not null && !limits.TryAcquire(session.UserId, AccountAction.Http, out var retryAfter))
                {
                    RateLimiting.SetRejectionHeaders(context, retryAfter);
                    return TypedResults.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Too many requests.");
                }

                await authentication.RevokeAsync(cookie, SessionClient.Web, cancellationToken);
            }

            context.Response.Cookies.Delete(SessionCookie.Name, SessionCookie.CreateOptions());
        }
        else
        {
            var request = await GameRequestReader.ReadAsync<LogoutRequest>(context.Request, cancellationToken);

            if (request is null || !(await validator.ValidateAsync(request, cancellationToken)).IsValid)
                return TypedResults.BadRequest(new LogoutResponse("Invalid logout request."));

            var session = await authentication.GetSessionForLogoutAsync(request.Session!, SessionClient.Game, cancellationToken);

            if (session is not null && !limits.TryAcquire(session.UserId, AccountAction.Http, out var retryAfter))
            {
                RateLimiting.SetRejectionHeaders(context, retryAfter);
                return TypedResults.Json(new LogoutResponse("Too many requests."), statusCode: StatusCodes.Status429TooManyRequests);
            }

            await authentication.RevokeAsync(request.Session!, SessionClient.Game, cancellationToken);
        }

        return TypedResults.Ok(new LogoutResponse());
    }
}
