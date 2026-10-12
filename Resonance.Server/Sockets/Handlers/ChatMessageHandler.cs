using Resonance.Core.Services;
using Resonance.Server.Services;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class ChatMessageHandler(ChatService chat, ChatChannels channels) : IPacketHandler
{
    public PacketType Type => PacketType.ChatMessage;

    public async ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var request = ChatPayload.Read<ChatMessagePacket>(payload);
        if (request is null)
            return;

        var message = await chat.CreateMessageAsync(connection.AccountId, request.RecipientId, request.Channel,
            request.Text, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (message is not null)
            channels.Send(connection, message);
    }
}
