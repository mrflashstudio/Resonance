using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class PingHandler : IPacketHandler
{
    public PacketType Type => PacketType.Ping;

    public ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        connection.Send(GamePacket.Create(PacketType.Pong));
        return ValueTask.CompletedTask;
    }
}
