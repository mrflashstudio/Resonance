using Resonance.Sockets.Protocol;

namespace Resonance.Sockets.Handlers;

public sealed class PingHandler : IPacketHandler
{
    public PacketType Type => PacketType.Ping;

    public void Handle(GameConnection connection)
    {
        connection.Send(GamePacket.Create(PacketType.Pong));
    }
}
