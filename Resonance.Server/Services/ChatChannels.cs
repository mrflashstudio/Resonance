using MessagePack;
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
        ["announce"] = new Channel(autoJoin: true, readOnly: true),
        ["resonance"] = new Channel(autoJoin: true, readOnly: false),
        ["english"] = new Channel(autoJoin: false, readOnly: false),
        ["german"] = new Channel(autoJoin: false, readOnly: false),
        ["russian"] = new Channel(autoJoin: false, readOnly: false)
    };

    private sealed class Channel(bool autoJoin, bool readOnly)
    {
        public bool AutoJoin { get; } = autoJoin;
        public bool ReadOnly { get; } = readOnly;
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

            var packet = GamePacket.Create(PacketType.ChatMessage, MessagePackSerializer.Serialize(new ChatMessagePacket
            {
                SenderId = message.SenderId,
                SenderName = message.SenderName,
                RecipientId = message.RecipientId,
                Channel = message.Channel,
                Text = message.Text,
                Badges = message.Badges.Length == 0 ? null : message.Badges,
            }));

            if (message.RecipientId is { } recipientId)
            {
                if (_connections.TryGetValue(recipientId, out var recipient) && !recipient.Stopped.IsCancellationRequested)
                    recipient.Send(packet);
                return;
            }

            if (message.Channel is null || !_channels.TryGetValue(message.Channel, out var target) ||
                target.ReadOnly || !target.Members.Contains(connection))
                return;

            foreach (var recipient in target.Members)
            {
                if (recipient != connection && !recipient.Stopped.IsCancellationRequested)
                    recipient.Send(packet);
            }
        }
    }
}
