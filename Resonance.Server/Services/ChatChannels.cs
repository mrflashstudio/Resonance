using MessagePack;
using Resonance.Core.Models;
using Resonance.Server.Sockets;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Services;

public sealed class ChatChannels
{
    public const string ResonanceChannel = "resonance";
    private readonly Lock _sync = new();
    private readonly Dictionary<int, GameConnection> _connections = [];
    private readonly HashSet<GameConnection> _members = [];

    private static void SendChannel(GameConnection connection, PacketType type)
    {
        connection.Send(GamePacket.Create(type, MessagePackSerializer.Serialize(new ChatChannelPacket(ResonanceChannel))));
    }

    public void Connect(GameConnection connection)
    {
        lock (_sync)
        {
            if (_connections.GetValueOrDefault(connection.AccountId) == connection)
                return;
            if (_connections.TryGetValue(connection.AccountId, out var previous))
                _members.Remove(previous);

            _connections[connection.AccountId] = connection;
            _members.Add(connection);
            SendChannel(connection, PacketType.ChannelAvailable);
            SendChannel(connection, PacketType.ChannelJoined);
        }
    }

    public void Disconnect(GameConnection connection)
    {
        lock (_sync)
        {
            _members.Remove(connection);
            if (_connections.GetValueOrDefault(connection.AccountId) == connection)
                _connections.Remove(connection.AccountId);
        }
    }

    public void SetMembership(GameConnection connection, string? channel, bool joined)
    {
        lock (_sync)
        {
            if (_connections.GetValueOrDefault(connection.AccountId) != connection ||
                !string.Equals(channel?.Trim(), ResonanceChannel, StringComparison.Ordinal))
                return;

            if (joined)
            {
                _members.Add(connection);
                SendChannel(connection, PacketType.ChannelJoined);
            }
            else
            {
                _members.Remove(connection);
                SendChannel(connection, PacketType.ChannelLeft);
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
            }));

            if (message.RecipientId is { } recipientId)
            {
                if (_connections.TryGetValue(recipientId, out var recipient) && !recipient.Stopped.IsCancellationRequested)
                    recipient.Send(packet);
                return;
            }

            if (message.Channel != ResonanceChannel || !_members.Contains(connection))
                return;

            foreach (var recipient in _members)
            {
                if (recipient != connection && !recipient.Stopped.IsCancellationRequested)
                    recipient.Send(packet);
            }
        }
    }
}
