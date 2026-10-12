using Resonance.Server.Services;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class OnlinePlayersHandler(IOnlinePlayers onlinePlayers) : IPacketHandler
{
    public PacketType Type => PacketType.OnlinePlayers;

    public ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        onlinePlayers.SendSnapshot(connection);
        return ValueTask.CompletedTask;
    }
}
