using System.Globalization;
using MessagePack;
using Resonance.Extensions;
using Resonance.Sockets.Models;
using Resonance.Sockets.Protocol;

namespace Resonance.Sockets.Handlers;

public sealed class ProfileHandler(TimeProvider clock) : IPacketHandler
{
    public PacketType Type => PacketType.Profile;

    public void Handle(GameConnection connection)
    {
        var account = connection.Account;
        var profile = new Profile
        {
            Id = account.Id,
            Uid = account.Id.ToString(CultureInfo.InvariantCulture),
            Username = account.Name,
            CreatedAt = account.CreatedAt.ToUnixTimeMilliseconds(),
            AboutMe = account.AboutMe,
            Flag = account.CountryCode,
            Position = account.Rank,
            SkillPoints = account.RhythmPoints,
            PlayCount = account.PlayCount,
            SquaresHit = account.SquaresHit,
            TotalScore = account.TotalScore,
            IsOnline = true,
            Verified = false,
            Ban = account.Status.ToProtocolString(account.StatusExpiresAt, clock.GetUtcNow()),
            Badges = []
        };

        connection.Send(GamePacket.Create(Type, MessagePackSerializer.Serialize(profile)));
    }
}
