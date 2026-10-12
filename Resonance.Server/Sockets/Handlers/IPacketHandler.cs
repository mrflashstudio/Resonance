using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public interface IPacketHandler
{
    PacketType Type { get; }
    ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}
