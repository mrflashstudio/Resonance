using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class PacketDispatcher(IEnumerable<IPacketHandler> handlers) : IPacketDispatcher
{
    private readonly IReadOnlyDictionary<PacketType, IPacketHandler> _handlers =
        handlers.ToDictionary(handler => handler.Type);

    public ValueTask DispatchAsync(PacketType type, GameConnection connection, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        return _handlers.TryGetValue(type, out var handler)
            ? handler.HandleAsync(connection, payload, cancellationToken)
            : ValueTask.CompletedTask;
    }
}
