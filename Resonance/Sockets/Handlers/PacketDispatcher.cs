using Resonance.Sockets.Protocol;

namespace Resonance.Sockets.Handlers;

public sealed class PacketDispatcher(IEnumerable<IPacketHandler> handlers) : IPacketDispatcher
{
    private readonly IReadOnlyDictionary<PacketType, IPacketHandler> _handlers =
        handlers.ToDictionary(handler => handler.Type);

    public void Dispatch(PacketType type, GameConnection connection)
    {
        if (_handlers.TryGetValue(type, out var handler))
            handler.Handle(connection);
    }
}
