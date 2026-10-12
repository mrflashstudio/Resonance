using Resonance.Core.Services;
using Resonance.Core.Models;
using Resonance.Server.Sockets;

namespace Resonance.Server.Services;

public interface IOnlinePlayers : IPresence
{
    void Register(GameConnection connection);
    bool Activate(GameConnection connection, Account account, CancellationToken cancellationToken);
    bool Disconnect(GameConnection connection);
    void SendSnapshot(GameConnection connection);
}
