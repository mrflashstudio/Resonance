using System.Net.WebSockets;
using Resonance.Models;

namespace Resonance.Sockets;

public interface IGameConnectionService
{
    Task RunAsync(WebSocket socket, AuthenticatedSession session, CancellationToken cancellationToken);
}
