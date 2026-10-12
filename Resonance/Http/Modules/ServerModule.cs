using System.Net.Http.Headers;
using Carter;
using Microsoft.Extensions.Options;
using Resonance.Core.Services;
using Resonance.Core.Models;
using Resonance.Http.Filters;
using Resonance.Server.Services;
using Resonance.Server.Sockets;
using Resonance.Configuration;

namespace Resonance.Http.Modules;

public sealed class ServerModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        var domain = endpoints.ServiceProvider.GetRequiredService<IOptions<ServerOptions>>().Value.Domain;

        endpoints.MapGet("/", ConnectAsync)
            .RequireHost($"production.{domain}", $"socket.{domain}", $"socketdev.{domain}");
    }

    private static async Task<IResult> ConnectAsync(
        HttpContext context, IOnlinePlayers onlinePlayers, IAuthenticationService authentication,
        IGameConnectionService connections, IOptions<ServerOptions> server, AccountActionLimiter limits,
        CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            EndpointFilters.DisableCaching(context.Response);

            return TypedResults.Content($"Resonance API<br><br>{onlinePlayers.Count} player(s) online<br><br>" +
                "<a href=\"https://github.com/mrflashstudio/Resonance\">Source Code</a>",
                "text/html", System.Text.Encoding.UTF8);
        }

        if (string.Equals(context.Request.Host.Host, $"production.{server.Value.Domain}", StringComparison.OrdinalIgnoreCase))
            return TypedResults.NotFound();

        if (!AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization) ||
            !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authorization.Parameter) ||
            await authentication.GetSessionAsync(authorization.Parameter, SessionClient.Game, cancellationToken) is not { } session)
            return TypedResults.Unauthorized();

        if (!limits.TryAcquire(session.UserId, AccountAction.Http, out var retryAfter))
        {
            RateLimiting.SetRejectionHeaders(context, retryAfter);
            return TypedResults.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Too many requests.");
        }

        using var pendingConnection = connections.TryBeginConnection(session.UserId);

        if (pendingConnection is null)
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "A game connection is already being established.");

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        await connections.RunAsync(socket, session, cancellationToken, pendingConnection);

        return TypedResults.Empty;
    }
}
