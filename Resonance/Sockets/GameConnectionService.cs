using System.Net.WebSockets;
using Microsoft.Extensions.Options;
using Resonance.Models;
using Resonance.Services;
using Resonance.Sockets.Handlers;
using Resonance.Sockets.Protocol;

namespace Resonance.Sockets;

public sealed class GameConnectionService(
    IPacketDispatcher dispatcher,
    IOnlinePlayers onlinePlayers,
    IAuthenticationService authentication,
    UserActivityService activity,
    IOptions<GameConnectionSettings> settings,
    TimeProvider clock,
    ILogger<GameConnectionService> logger)
    : IGameConnectionService
{
    private const int MaximumPacketSize = 64 * 1024;

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

    public async Task RunAsync(WebSocket socket, AuthenticatedSession session, CancellationToken cancellationToken)
    {
        var account = session.Account;
        var buffer = new byte[MaximumPacketSize];
        var length = 0;
        using var connection = new GameConnection(account, session.Id);
        using var expiry = new CancellationTokenSource(Timeout.InfiniteTimeSpan, clock);
        using var inactivity = new CancellationTokenSource(Timeout.InfiniteTimeSpan, clock);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, connection.Stopped, expiry.Token, inactivity.Token);
        var token = lifetime.Token;
        var sender = SendAsync(socket, connection, lifetime);
        var closeStatus = WebSocketCloseStatus.NormalClosure;
        string? closeDescription = null;

        try
        {
            onlinePlayers.Connect(connection);

            var active = await authentication.IsSessionActiveAsync(session.Id, token);
            var remaining = session.ExpiresAt - clock.GetUtcNow();

            if (remaining <= TimeSpan.Zero || !active)
                await lifetime.CancelAsync();
            else
            {
                expiry.CancelAfter(remaining);
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

                if (!GamePacket.TryRead(buffer.AsSpan(0, length), out var type))
                {
                    closeStatus = WebSocketCloseStatus.InvalidPayloadData;
                    closeDescription = "Invalid packet.";
                    break;
                }

                length = 0;

                dispatcher.Dispatch(type, connection);
                inactivity.CancelAfter(settings.Value.InactivityTimeout);

                logger.LogDebug("Received game packet {PacketType} for account {AccountId}", type, account.Id);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (WebSocketException)
        {
            logger.LogDebug("Game connection closed for account {AccountId}", account.Id);
        }
        finally
        {
            var disconnectedAt = clock.GetUtcNow();
            var lastConnection = onlinePlayers.Disconnect(connection);
            await lifetime.CancelAsync();
            await sender;

            if (lastConnection)
                await activity.RecordAsync(account.Id, disconnectedAt);
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
