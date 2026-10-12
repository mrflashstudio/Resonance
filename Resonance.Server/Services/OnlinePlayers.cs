using MessagePack;
using Resonance.Core.Models;
using Resonance.Server.Extensions;
using Resonance.Server.Sockets;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Services;

public sealed class OnlinePlayers(TimeProvider clock) : IOnlinePlayers
{
    private sealed record Player(Account Account, GameConnection Connection);

    private readonly Lock _sync = new();
    private readonly Dictionary<int, Player> _players = [];
    private readonly HashSet<GameConnection> _connections = [];

    private OnlinePlayer CreateDetails(Account account)
    {
        return new OnlinePlayer(account.Id, account.Name,
            Ban: account.Status.ToProtocolString(account.StatusExpiresAt, clock.GetUtcNow()),
            Position: account.Rank, SkillPoints: account.RhythmPoints, PlayCount: account.PlayCount, Flag: account.CountryCode);
    }

    private void Broadcast(PacketType type, OnlinePlayer player, GameConnection? excluded = null)
    {
        var packet = GamePacket.Create(type, MessagePackSerializer.Serialize(player));

        foreach (var recipient in _players.Values.Select(entry => entry.Connection))
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

    public void Register(GameConnection connection)
    {
        lock (_sync)
            _connections.Add(connection);
    }

    public bool Activate(GameConnection connection, Account account, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_connections.Contains(connection) || connection.Stopped.IsCancellationRequested ||
                cancellationToken.IsCancellationRequested || account.Id != connection.AccountId)
                return false;

            if (_players.TryGetValue(account.Id, out var existing))
            {
                _players[account.Id] = new Player(account, connection);
                if (existing.Connection != connection)
                    existing.Connection.Stop();
                return true;
            }

            var details = CreateDetails(account);
            _players.Add(account.Id, new Player(account, connection));
            Broadcast(PacketType.PlayerJoined, details, connection);
            return true;
        }
    }

    public bool Disconnect(GameConnection connection)
    {
        lock (_sync)
        {
            _connections.Remove(connection);

            if (!_players.TryGetValue(connection.AccountId, out var player) ||
                player.Connection != connection)
                return false;

            _players.Remove(connection.AccountId);
            Broadcast(PacketType.PlayerLeft, CreateDetails(player.Account));
            return true;
        }
    }

    public void SendSnapshot(GameConnection connection)
    {
        lock (_sync)
        {
            if (!_players.TryGetValue(connection.AccountId, out var player) ||
                player.Connection != connection)
                return;

            var snapshot = _players.Values.Select(entry => CreateDetails(entry.Account)).OrderBy(entry => entry.Id).ToArray();
            connection.Send(GamePacket.Create(PacketType.OnlinePlayers, MessagePackSerializer.Serialize(snapshot)));
        }
    }

    public void DisconnectSession(Guid sessionId)
    {
        lock (_sync)
        {
            foreach (var connection in _connections)
            {
                if (connection.SessionId == sessionId)
                    connection.Stop();
            }
        }
    }
}

