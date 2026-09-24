using Resonance.Sockets.Protocol;

namespace Resonance.Sockets.Handlers;

public interface IPacketDispatcher
{
    void Dispatch(PacketType type, GameConnection connection);
}
