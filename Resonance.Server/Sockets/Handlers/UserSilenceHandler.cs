using Resonance.Core.Services;
using Resonance.Server.Services;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class UserSilenceHandler(ModerationService moderation, OnlinePlayers players, ChatChannels channels)
    : IPacketHandler, IDisposable
{
    private readonly SemaphoreSlim _changes = new(1, 1);

    public PacketType Type => PacketType.UserSilence;

    public async ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var request = PacketPayload.Read<UserSilencePacket>(payload, fieldCount: 4);

        if (request is null || !channels.IsConnected(connection))
            return;

        DateTimeOffset? expiresAt = null;

        if (request.Silenced && request.ExpiresAt is { } expiry)
        {
            if (expiry < DateTimeOffset.MinValue.ToUnixTimeMilliseconds() ||
                expiry > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds())
                return;

            expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiry);
        }

        await _changes.WaitAsync(cancellationToken);

        try
        {
            if (!channels.IsConnected(connection))
                return;

            var account = await moderation.ChangeSilenceAsync(connection.AccountId, request.UserId,
                request.Silenced, expiresAt, request.Reason, cancellationToken);

            if (account is not null)
                players.UpdateModeration(account);
        }
        finally
        {
            _changes.Release();
        }
    }

    public void Dispose()
    {
        _changes.Dispose();
    }
}
