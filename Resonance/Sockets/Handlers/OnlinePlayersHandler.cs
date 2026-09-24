using Resonance.Services;
using Resonance.Sockets.Protocol;

namespace Resonance.Sockets.Handlers;

public sealed class OnlinePlayersHandler(IOnlinePlayers onlinePlayers) : IPacketHandler
{
    public PacketType Type => PacketType.OnlinePlayers;

    public void Handle(GameConnection connection)
    {
        onlinePlayers.SendSnapshot(connection);
    }
}
