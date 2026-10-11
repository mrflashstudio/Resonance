using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Carter;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Resonance.Authentication;
using Resonance.Extensions;
using Resonance.Http.Filters;
using Resonance.Models;
using Resonance.Routing;
using Resonance.Services;

namespace Resonance.Http.Modules;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapProductionApi();

        api.MapPost("/loginAccount", LoginAsync).AddEndpointFilter(EndpointFilters.NoStoreAsync);
        api.MapPost("/registerAccount", RegisterAsync);
        api.MapGet("/session", GetAsync).AddEndpointFilter(EndpointFilters.NoStoreAsync);
        api.MapPost("/logoutAccount", LogoutAsync).AddEndpointFilter(EndpointFilters.NoStoreAsync);
    }

    private static async Task<Results<Ok<LoginResponse>, BadRequest<LoginResponse>, JsonHttpResult<LoginResponse>>> LoginAsync(
        HttpContext context, IAuthenticationService authentication, IOptions<ServerOptions> server,
        IOptions<SessionSettings> sessions, CancellationToken cancellationToken)
    {
        LoginRequest? login;

        try
        {
            login = await JsonSerializer.DeserializeAsync<LoginRequest>(
                context.Request.Body, JsonSerializerOptions.Web, cancellationToken);
        }
        catch (JsonException)
        {
            return TypedResults.BadRequest(new LoginResponse("Invalid login request."));
        }

        if (login is null || string.IsNullOrWhiteSpace(login.Login) ||
            string.IsNullOrEmpty(login.PasswordProof) || string.IsNullOrEmpty(login.Client))
            return TypedResults.BadRequest(new LoginResponse("Invalid login request."));

        if (login.Client == "web" && !context.Request.HasTrustedOrigin(server.Value))
            return TypedResults.Json(new LoginResponse("Invalid origin."), statusCode: StatusCodes.Status403Forbidden);

        var response = await authentication.LoginAsync(login, cancellationToken);

        if (login.Client == "web" && response.Session is { } token)
        {
            if (context.Request.Cookies.TryGetValue(SessionCookie.Name, out var previous))
                await authentication.RevokeAsync(previous, "web", cancellationToken);

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

        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(registration, new ValidationContext(registration), errors, true))
            return TypedResults.ValidationProblem(errors
                .SelectMany(error => error.MemberNames.Select(member => (Member: member, Message: error.ErrorMessage!)))
                .GroupBy(error => error.Member)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray()));

        var result = await authentication.RegisterAsync(registration, cancellationToken);

        return result is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Username is already taken.")
            : TypedResults.Created((string?)null, result);
    }

    private static async Task<Results<Ok<Account>, ProblemHttpResult>> GetAsync(
        HttpRequest request, IAuthenticationService authentication, CancellationToken cancellationToken)
    {
        if (!request.Cookies.TryGetValue(SessionCookie.Name, out var token) ||
            await authentication.GetSessionAsync(token, "web", cancellationToken) is not { } session)
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);

        return TypedResults.Ok(session.Account);
    }

    private static async Task<Results<Ok<LogoutResponse>, BadRequest<LoginResponse>, ProblemHttpResult>> LogoutAsync(
        HttpContext context, IAuthenticationService authentication, IOptions<ServerOptions> server,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("Origin") || context.Request.Cookies.ContainsKey(SessionCookie.Name))
        {
            if (!context.Request.HasTrustedOrigin(server.Value))
                return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden);

            if (context.Request.Cookies.TryGetValue(SessionCookie.Name, out var cookie))
                await authentication.RevokeAsync(cookie, "web", cancellationToken);

            context.Response.Cookies.Delete(SessionCookie.Name, SessionCookie.CreateOptions());
        }
        else
        {
            LogoutRequest? request;

            try
            {
                request = await JsonSerializer.DeserializeAsync<LogoutRequest>(
                    context.Request.Body, JsonSerializerOptions.Web, cancellationToken);
            }
            catch (JsonException)
            {
                return TypedResults.BadRequest(new LoginResponse("Invalid logout request."));
            }

            if (string.IsNullOrEmpty(request?.Session))
                return TypedResults.BadRequest(new LoginResponse("Invalid logout request."));

            await authentication.RevokeAsync(request.Session, "game", cancellationToken);
        }

        return TypedResults.Ok(new LogoutResponse());
    }
}
