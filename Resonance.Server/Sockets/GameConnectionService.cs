using System.Net.WebSockets;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resonance.Core.Models;
using Resonance.Core.Services;
using Resonance.Server.Services;
using Resonance.Server.Configuration;
using Resonance.Server.Sockets.Handlers;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets;

public sealed class GameConnectionService(
    IPacketDispatcher dispatcher,
    IOnlinePlayers onlinePlayers,
    IAuthenticationService authentication,
    IProfileService profiles,
    UserActivityService activity,
    IOptions<GameConnectionSettings> settings,
    TimeProvider clock,
    ILogger<GameConnectionService> logger,
    AccountActionLimiter limits)
    : IGameConnectionService
{
    private const int MaximumPacketSize = 64 * 1024;
    private static readonly TimeSpan AuthenticationTimeout = TimeSpan.FromSeconds(10);
    private readonly ConcurrentDictionary<int, PendingGameConnection> _pending = new();

    public PendingGameConnection? TryBeginConnection(int accountId)
    {
        PendingGameConnection? pendingConnection = null;
        pendingConnection = new PendingGameConnection(accountId, () => _pending.TryRemove(new KeyValuePair<int, PendingGameConnection>(accountId, pendingConnection!)));
        return _pending.TryAdd(accountId, pendingConnection) ? pendingConnection : null;
    }

    private async Task<bool> ValidateAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(AuthenticationTimeout, clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            return await authentication.IsSessionActiveAsync(sessionId, linked.Token).WaitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not validate session {SessionId}", sessionId);
            return false;
        }
    }

    private static async Task SendAsync(WebSocket socket, GameConnection connection, CancellationTokenSource lifetime)
    {
        try
        {
            await foreach (var packet in connection.Outgoing.ReadAllAsync(lifetime.Token))
                await socket.SendAsync(packet.AsMemory(), WebSocketMessageType.Binary, true, lifetime.Token);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            await lifetime.CancelAsync();
        }
    }

    public async Task RunAsync(WebSocket socket, AuthenticatedSession session, CancellationToken cancellationToken,
        PendingGameConnection? pendingConnection = null)
    {
        using var reservation = pendingConnection ?? TryBeginConnection(session.UserId);
        if (reservation is null || reservation.AccountId != session.UserId ||
            !_pending.TryGetValue(session.UserId, out var pending) || pending != reservation)
        {
            socket.Abort();
            return;
        }

        var buffer = new byte[MaximumPacketSize];
        var length = 0;
        using var connection = new GameConnection(session.UserId, session.Id);
        using var inactivity = new CancellationTokenSource(Timeout.InfiniteTimeSpan, clock);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, connection.Stopped, inactivity.Token);
        var token = lifetime.Token;
        var sender = SendAsync(socket, connection, lifetime);
        var closeStatus = WebSocketCloseStatus.NormalClosure;
        string? closeDescription = null;

        try
        {
            onlinePlayers.Register(connection);

            Account? account = null;
            using (var deadline = new CancellationTokenSource(AuthenticationTimeout, clock))
            using (var connectionAttemptLifetime = CancellationTokenSource.CreateLinkedTokenSource(token, deadline.Token))
            {
                try
                {
                    var active = await authentication.IsSessionActiveAsync(session.Id, connectionAttemptLifetime.Token)
                        .WaitAsync(connectionAttemptLifetime.Token);
                    account = active ? await profiles.GetAsync(session.UserId, connectionAttemptLifetime.Token).WaitAsync(connectionAttemptLifetime.Token) : null;

                    if (account is null || authentication.GetSessionExpiry(session.Id) is null ||
                        !onlinePlayers.Activate(connection, account, connectionAttemptLifetime.Token))
                        account = null;
                }
                catch (OperationCanceledException) when (connectionAttemptLifetime.IsCancellationRequested)
                {
                    account = null;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not establish game connection for account {AccountId}", session.UserId);
                    account = null;
                }
            }
            reservation.Dispose();

            if (account is null)
                await lifetime.CancelAsync();
            else
            {
                inactivity.CancelAfter(settings.Value.InactivityTimeout);
                await activity.RecordAsync(account.Id, clock.GetUtcNow());
            }

            while (socket.State == WebSocketState.Open)
            {
                var received = await socket.ReceiveAsync(buffer.AsMemory(length), token);

                if (received.MessageType == WebSocketMessageType.Close)
                    break;

                length += received.Count;

                if (received.MessageType != WebSocketMessageType.Binary ||
                    (length == buffer.Length && !received.EndOfMessage))
                {
                    closeStatus = WebSocketCloseStatus.InvalidMessageType;
                    closeDescription = "Expected a bounded binary packet.";
                    break;
                }

                if (!received.EndOfMessage)
                    continue;

                if (!GamePacket.TryRead(buffer.AsMemory(0, length), out var type, out var payload))
                {
                    closeStatus = WebSocketCloseStatus.InvalidPayloadData;
                    closeDescription = "Invalid packet.";
                    break;
                }

                if (!await ValidateAsync(session.Id, token))
                    break;

                if (!limits.TryAcquire(session.UserId, AccountAction.GamePacket, out _))
                {
                    closeStatus = WebSocketCloseStatus.PolicyViolation;
                    closeDescription = "Too many packets.";
                    break;
                }

                token.ThrowIfCancellationRequested();
                await dispatcher.DispatchAsync(type, connection, payload, token);
                length = 0;
                inactivity.CancelAfter(settings.Value.InactivityTimeout);

                logger.LogDebug("Received game packet {PacketType} for account {AccountId}", type, session.UserId);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (WebSocketException)
        {
            logger.LogDebug("Game connection closed for account {AccountId}", session.UserId);
        }
        finally
        {
            var disconnectedAt = clock.GetUtcNow();
            var lastConnection = onlinePlayers.Disconnect(connection);
            await lifetime.CancelAsync();
            await sender;

            if (lastConnection)
                await activity.RecordAsync(session.UserId, disconnectedAt);
        }

        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5), clock);

            try
            {
                await socket.CloseOutputAsync(closeStatus, closeDescription, timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                socket.Abort();
            }
            catch (WebSocketException)
            {
                socket.Abort();
            }
        }
    }
}
