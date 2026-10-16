using Resonance.Server.Services;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class JoinChannelHandler(ChatChannels channels) : IPacketHandler
{
    public PacketType Type => PacketType.JoinChannel;

    public ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var request = PacketPayload.Read<ChatChannelPacket>(payload);
        if (request is not null)
            channels.SetMembership(connection, request.Name, joined: true);
        
        return ValueTask.CompletedTask;
    }
}
