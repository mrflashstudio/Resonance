using MessagePack;
using Resonance.Extensions;
using Resonance.Sockets.Protocol;

namespace Resonance.Sockets.Handlers;

public sealed class ProfileHandler(TimeProvider clock) : IPacketHandler
{
    public PacketType Type => PacketType.Profile;

    public void Handle(GameConnection connection)
    {
        var profile = connection.Account.ToProfile(clock.GetUtcNow()) with { IsOnline = true };

        connection.Send(GamePacket.Create(Type, MessagePackSerializer.Serialize(profile)));
    }
}
