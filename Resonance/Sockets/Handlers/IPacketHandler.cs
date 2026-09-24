using Resonance.Sockets.Protocol;

namespace Resonance.Sockets.Handlers;

public interface IPacketHandler
{
    PacketType Type { get; }
    void Handle(GameConnection connection);
}
