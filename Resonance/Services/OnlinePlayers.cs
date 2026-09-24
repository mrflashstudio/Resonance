using MessagePack;
using Resonance.Extensions;
using Resonance.Models;
using Resonance.Sockets;
using Resonance.Sockets.Models;
using Resonance.Sockets.Protocol;

namespace Resonance.Services;

public sealed class OnlinePlayers(TimeProvider clock) : IOnlinePlayers
{
    private sealed record Player(Account Account, HashSet<GameConnection> Connections);

    private readonly Lock _sync = new();
    private readonly Dictionary<int, Player> _players = [];

    private OnlinePlayer CreateDetails(Account account)
    {
        return new OnlinePlayer(account.Id, account.Name,
            Ban: account.Status.ToProtocolString(account.StatusExpiresAt, clock.GetUtcNow()),
            Position: account.Rank, SkillPoints: account.RhythmPoints, PlayCount: account.PlayCount, Flag: account.CountryCode);
    }

    private void Broadcast(PacketType type, OnlinePlayer player, GameConnection? excluded = null)
    {
        var packet = GamePacket.Create(type, MessagePackSerializer.Serialize(player));

        foreach (var recipient in _players.Values.SelectMany(entry => entry.Connections))
        {
            if (recipient != excluded)
                recipient.Send(packet);
        }
    }

    public int Count
    {
        get
        {
            lock (_sync)
                return _players.Count;
        }
    }

    public bool IsOnline(int userId)
    {
        lock (_sync)
            return _players.ContainsKey(userId);
    }

    public void Connect(GameConnection connection)
    {
        lock (_sync)
        {
            var account = connection.Account;

            if (_players.TryGetValue(account.Id, out var existing))
            {
                existing.Connections.Add(connection);
                return;
            }

            var details = CreateDetails(account);
            _players.Add(account.Id, new Player(account, [connection]));
            Broadcast(PacketType.PlayerJoined, details, connection);
        }
    }

    public bool Disconnect(GameConnection connection)
    {
        lock (_sync)
        {
            if (!_players.TryGetValue(connection.Account.Id, out var player) ||
                !player.Connections.Remove(connection) || player.Connections.Count != 0)
                return false;

            _players.Remove(connection.Account.Id);
            Broadcast(PacketType.PlayerLeft, CreateDetails(player.Account));
            return true;
        }
    }

    public void SendSnapshot(GameConnection connection)
    {
        lock (_sync)
        {
            if (!_players.TryGetValue(connection.Account.Id, out var player) ||
                !player.Connections.Contains(connection))
                return;

            var snapshot = _players.Values.Select(entry => CreateDetails(entry.Account)).OrderBy(entry => entry.Id).ToArray();
            connection.Send(GamePacket.Create(PacketType.OnlinePlayers, MessagePackSerializer.Serialize(snapshot)));
        }
    }

    public void DisconnectSession(Guid sessionId)
    {
        lock (_sync)
        {
            foreach (var connection in _players.Values.SelectMany(player => player.Connections))
            {
                if (connection.SessionId == sessionId)
                    connection.Stop();
            }
        }
    }
}
