using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class PacketDispatcher : IPacketDispatcher, IDisposable
{
    private readonly Dictionary<PacketType, IPacketHandler> _handlers = [];
    private bool _disposed;

    public PacketDispatcher(IServiceProvider services, Assembly? handlerAssembly = null)
    {
        var handlers = new List<IPacketHandler>();
        try
        {
            var types = (handlerAssembly ?? typeof(IPacketHandler).Assembly).GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && !type.ContainsGenericParameters && 
                               typeof(IPacketHandler).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

            foreach (var type in types)
            {
                var handler = (IPacketHandler)ActivatorUtilities.CreateInstance(services, type);
                handlers.Add(handler);
                
                if (!_handlers.TryAdd(handler.Type, handler))
                    throw new InvalidOperationException($"Multiple packet handlers use packet ID {(byte)handler.Type} ({handler.Type}).");
            }
        }
        catch
        {
            foreach (var handler in handlers.OfType<IDisposable>())
                handler.Dispose();
            
            throw;
        }
    }

    public ValueTask DispatchAsync(PacketType type, GameConnection connection, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        return _handlers.TryGetValue(type, out var handler)
            ? handler.HandleAsync(connection, payload, cancellationToken)
            : ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        foreach (var handler in _handlers.Values.OfType<IDisposable>())
            handler.Dispose();
    }
}
