using Resonance.Core.Commands;
using Resonance.Core.Services;
using Resonance.Server.Services;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class ChatMessageHandler(ChatService chat, ChatChannels channels, ServerBotService bot) : IPacketHandler
{
    public PacketType Type => PacketType.ChatMessage;

    public async ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var request = PacketPayload.Read<ChatMessagePacket>(payload);
        if (request is null || !channels.IsConnected(connection))
            return;

        var message = await chat.CreateMessageAsync(connection.AccountId, request.RecipientId, request.Channel,
            request.Text, cancellationToken);
        
        cancellationToken.ThrowIfCancellationRequested();
        
        if (message is null)
            return;

        if (bot.CanHandle(message))
        {
            if (!channels.IsConnected(connection) || (message.Channel is not null && !channels.CanPost(connection, message)))
                return;
            
            if (message.Channel is not null && bot.GetInvocationVisibility(message) == CommandVisibility.Channel)
                channels.Send(connection, message);
            
            var reply = await bot.ReplyAsync(message, cancellationToken);
            if (reply is not null)
                channels.SendBotReply(connection, reply, message);
            
            return;
        }

        channels.Send(connection, message);
    }
}
