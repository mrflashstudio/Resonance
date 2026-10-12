using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public interface IPacketDispatcher
{
    ValueTask DispatchAsync(PacketType type, GameConnection connection, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken);
}
