using MessagePack;
using Resonance.Core.Services;
using Resonance.Server.Extensions;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class ProfileHandler(IProfileService profiles, TimeProvider clock, IProfileAssets assets) : IPacketHandler
{
    public PacketType Type => PacketType.Profile;

    public async ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var account = await profiles.GetAsync(connection.AccountId, cancellationToken);

        if (account is null)
        {
            connection.Stop();
            return;
        }

        var profile = account.ToProfile(clock.GetUtcNow(), assets);

        connection.Send(GamePacket.Create(Type, MessagePackSerializer.Serialize(profile)));
    }
}
