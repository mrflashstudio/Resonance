using System.Net.Http.Headers;
using Carter;
using Microsoft.Extensions.Options;
using Resonance.Http.Filters;
using Resonance.Routing;
using Resonance.Services;
using Resonance.Sockets;

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
        IGameConnectionService connections, IOptions<ServerOptions> server, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            EndpointFilters.DisableCaching(context.Response);

            return TypedResults.Content($"Resonance API<br><br>{onlinePlayers.Count} player(s) online<br><br>\r\n" +
                "            <a href=\"https://github.com/mrflashstudio/Resonance\">Source Code</a>",
                "text/html", System.Text.Encoding.UTF8);
        }

        if (string.Equals(context.Request.Host.Host, $"production.{server.Value.Domain}", StringComparison.OrdinalIgnoreCase))
            return TypedResults.NotFound();

        if (!AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization) ||
            !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authorization.Parameter) ||
            await authentication.GetSessionAsync(authorization.Parameter, "game", cancellationToken) is not { } session)
            return TypedResults.Unauthorized();

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        await connections.RunAsync(socket, session, cancellationToken);

        return TypedResults.Empty;
    }
}
