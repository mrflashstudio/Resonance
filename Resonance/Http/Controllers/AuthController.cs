using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Resonance.Authentication;
using Resonance.Extensions;
using Resonance.Models;
using Resonance.Routing;
using Resonance.Services;

namespace Resonance.Http.Controllers;

[ApiController]
[Subdomain("production")]
[Route("api")]
[RequestSizeLimit(4096)]
public sealed class AuthController(
    IAuthenticationService authentication,
    IOptions<ServerOptions> server,
    IOptions<SessionSettings> sessions) : ControllerBase
{
    [HttpPost("loginAccount")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<LoginResponse>> LoginAsync(CancellationToken cancellationToken)
    {
        LoginRequest? login;

        try
        {
            login = await JsonSerializer.DeserializeAsync<LoginRequest>(
                Request.Body, JsonSerializerOptions.Web, cancellationToken);
        }
        catch (JsonException)
        {
            return BadRequest(new LoginResponse("Invalid login request."));
        }

        if (login is null || string.IsNullOrWhiteSpace(login.Login) ||
            string.IsNullOrEmpty(login.PasswordProof) || string.IsNullOrEmpty(login.Client))
            return BadRequest(new LoginResponse("Invalid login request."));

        if (login.Client == "web" && !Request.HasTrustedOrigin(server.Value))
            return StatusCode(StatusCodes.Status403Forbidden, new LoginResponse("Invalid origin."));

        var response = await authentication.LoginAsync(login, cancellationToken);

        if (login.Client == "web" && response.Session is { } token)
        {
            if (Request.Cookies.TryGetValue(SessionCookie.Name, out var previous))
                await authentication.RevokeAsync(previous, "web", cancellationToken);

            Response.Cookies.Append(SessionCookie.Name, token, SessionCookie.CreateOptions(sessions.Value.Lifetime));
            response = response with { Session = null };
        }

        return Ok(response);
    }

    [HttpPost("registerAccount")]
    public async Task<ActionResult<RegistrationResponse>> RegisterAsync(
        RegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await authentication.RegisterAsync(request, cancellationToken);

        return result is null
            ? Problem(statusCode: StatusCodes.Status409Conflict, title: "Username is already taken.")
            : StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("session")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<Account>> GetAsync(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(SessionCookie.Name, out var token) ||
            await authentication.GetSessionAsync(token, "web", cancellationToken) is not { } session)
            return Unauthorized();

        return Ok(session.Account);
    }

    [HttpPost("logoutAccount")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        if (Request.Headers.ContainsKey("Origin") || Request.Cookies.ContainsKey(SessionCookie.Name))
        {
            if (!Request.HasTrustedOrigin(server.Value))
                return StatusCode(StatusCodes.Status403Forbidden);

            if (Request.Cookies.TryGetValue(SessionCookie.Name, out var cookie))
                await authentication.RevokeAsync(cookie, "web", cancellationToken);
            Response.Cookies.Delete(SessionCookie.Name, SessionCookie.CreateOptions());
        }
        else
        {
            LogoutRequest? request;

            try
            {
                request = await JsonSerializer.DeserializeAsync<LogoutRequest>(
                    Request.Body, JsonSerializerOptions.Web, cancellationToken);
            }
            catch (JsonException)
            {
                return BadRequest(new LoginResponse("Invalid logout request."));
            }

            if (string.IsNullOrEmpty(request?.Session))
                return BadRequest(new LoginResponse("Invalid logout request."));

            await authentication.RevokeAsync(request.Session, "game", cancellationToken);
        }

        return Ok(new { error = (string?)null });
    }
}
