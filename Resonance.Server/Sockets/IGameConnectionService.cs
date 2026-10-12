using System.Net.WebSockets;
using Resonance.Core.Models;

namespace Resonance.Server.Sockets;

public interface IGameConnectionService
{
    PendingGameConnection? TryBeginConnection(int accountId);
    Task RunAsync(WebSocket socket, AuthenticatedSession session, CancellationToken cancellationToken,
        PendingGameConnection? pendingConnection = null);
}
