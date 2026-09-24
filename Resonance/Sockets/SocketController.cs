using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Resonance.Routing;
using Resonance.Services;

namespace Resonance.Sockets;

[ApiController]
[Subdomain("socket", "socketdev")]
[WebSocketRequest(true)]
[Route("/")]
public sealed class SocketController(IAuthenticationService authentication, IGameConnectionService connections)
    : ControllerBase
{
    [HttpGet]
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authorization) ||
            !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authorization.Parameter) ||
            await authentication.GetSessionAsync(authorization.Parameter, "game", cancellationToken) is not { } session)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        await connections.RunAsync(socket, session, cancellationToken);
    }
}
