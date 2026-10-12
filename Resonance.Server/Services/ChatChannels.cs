using MessagePack;
using Resonance.Core.Commands;
using Resonance.Core.Extensions;
using Resonance.Core.Models;
using Resonance.Server.Sockets;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Services;

public sealed class ChatChannels
{
    private readonly Lock _sync = new();
    private readonly Dictionary<int, GameConnection> _connections = [];
    private readonly Dictionary<string, Channel> _channels = new(StringComparer.Ordinal)
    {
        ["announce"] = new Channel(autoJoin: true, postPrivileges: UserPrivileges.Moderator),
        ["resonance"] = new Channel(autoJoin: true),
        ["english"] = new Channel(autoJoin: false),
        ["german"] = new Channel(autoJoin: false),
        ["russian"] = new Channel(autoJoin: false)
    };

    private sealed class Channel(bool autoJoin, UserPrivileges? postPrivileges = null)
    {
        public bool AutoJoin { get; } = autoJoin;
        public UserPrivileges? PostPrivileges { get; } = postPrivileges;
        public HashSet<GameConnection> Members { get; } = [];
    }

    private static void SendChannel(GameConnection connection, PacketType type, string name)
    {
        connection.Send(GamePacket.Create(type, MessagePackSerializer.Serialize(new ChatChannelPacket(name))));
    }

    public void Connect(GameConnection connection)
    {
        lock (_sync)
        {
            if (_connections.GetValueOrDefault(connection.AccountId) == connection)
                return;
            if (_connections.TryGetValue(connection.AccountId, out var previous))
                foreach (var channel in _channels.Values)
                    channel.Members.Remove(previous);

            _connections[connection.AccountId] = connection;
            foreach (var name in _channels.Keys)
                SendChannel(connection, PacketType.ChannelAvailable, name);
            foreach (var (name, channel) in _channels)
            {
                if (!channel.AutoJoin)
                    continue;
                channel.Members.Add(connection);
                SendChannel(connection, PacketType.ChannelJoined, name);
            }
        }
    }

    public void Disconnect(GameConnection connection)
    {
        lock (_sync)
        {
            foreach (var channel in _channels.Values)
                channel.Members.Remove(connection);
            if (_connections.GetValueOrDefault(connection.AccountId) == connection)
                _connections.Remove(connection.AccountId);
        }
    }

    public bool IsConnected(GameConnection connection)
    {
        lock (_sync)
            return _connections.GetValueOrDefault(connection.AccountId) == connection && !connection.Stopped.IsCancellationRequested;
    }

    public void SetMembership(GameConnection connection, string? channel, bool joined)
    {
        lock (_sync)
        {
            if (_connections.GetValueOrDefault(connection.AccountId) != connection ||
                connection.Stopped.IsCancellationRequested || channel is null ||
                !_channels.TryGetValue(channel.Trim(), out var target))
                return;

            if (joined)
            {
                target.Members.Add(connection);
                SendChannel(connection, PacketType.ChannelJoined, channel.Trim());
            }
            else
            {
                target.Members.Remove(connection);
                SendChannel(connection, PacketType.ChannelLeft, channel.Trim());
            }
        }
    }

    public void Send(GameConnection connection, ChatMessage message)
    {
        lock (_sync)
        {
            if (_connections.GetValueOrDefault(connection.AccountId) != connection ||
                connection.Stopped.IsCancellationRequested || message.SenderId != connection.AccountId)
                return;

            var packet = CreateMessagePacket(message);

            if (message.RecipientId is { } recipientId)
            {
                if (_connections.TryGetValue(recipientId, out var recipient) && !recipient.Stopped.IsCancellationRequested)
                    recipient.Send(packet);
                return;
            }

            if (message.Channel is not { } channel || !CanPostToChannel(connection, message.Sender, channel))
                return;

            foreach (var recipient in _channels[channel].Members)
            {
                if (recipient != connection && !recipient.Stopped.IsCancellationRequested)
                    recipient.Send(packet);
            }
        }
    }

    public bool CanPost(GameConnection connection, ChatMessage message)
    {
        lock (_sync)
            return message.SenderId == connection.AccountId && CanPostToChannel(connection, message.Sender, message.Channel);
    }

    private bool CanPostToChannel(GameConnection connection, Account sender, string? channel) =>
        _connections.GetValueOrDefault(connection.AccountId) == connection && !connection.Stopped.IsCancellationRequested &&
        channel is not null && _channels.TryGetValue(channel, out var target) && target.Members.Contains(connection) &&
        (target.PostPrivileges is not { } privileges || sender.HasAnyPrivilege(privileges));

    public void SendBotReply(GameConnection connection, BotReply reply, ChatMessage request)
    {
        lock (_sync)
        {
            if (_connections.GetValueOrDefault(connection.AccountId) != connection || connection.Stopped.IsCancellationRequested ||
                !reply.Message.Sender.IsBot || request.SenderId != connection.AccountId ||
                (request.Channel is not null && !CanPostToChannel(connection, request.Sender, request.Channel)))
                return;

            var message = reply.Message;
            var packet = CreateMessagePacket(message);
            if (request.Channel is null)
            {
                if (message.RecipientId == connection.AccountId && message.Channel is null)
                    connection.Send(packet);
                return;
            }

            if (message.RecipientId is not null || message.Channel != request.Channel)
                return;
            if (reply.Visibility == CommandVisibility.SenderOnly)
                connection.Send(packet);
            else if (reply.Visibility == CommandVisibility.Channel)
            {
                foreach (var recipient in _channels[request.Channel].Members)
                {
                    if (!recipient.Stopped.IsCancellationRequested)
                        recipient.Send(packet);
                }
            }
        }
    }

    private static byte[] CreateMessagePacket(ChatMessage message)
    {
        return GamePacket.Create(PacketType.ChatMessage, MessagePackSerializer.Serialize(new ChatMessagePacket
        {
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            RecipientId = message.RecipientId,
            Channel = message.Channel,
            Text = message.Text,
            Badges = message.Badges.Length == 0 ? null : message.Badges
        }));
    }
}
