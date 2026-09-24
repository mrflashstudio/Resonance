using MessagePack;

namespace Resonance.Sockets.Models;

[MessagePackObject]
public sealed record OnlinePlayer(
    [property: Key(0)] int Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string? ProfilePictureUrl = null,
    [property: Key(3)] string? Ban = null,
    [property: Key(4)] int? Position = 0,
    [property: Key(5)] double? SkillPoints = 0,
    [property: Key(6)] int? PlayCount = 0,
    [property: Key(7)] string? Flag = null,
    [property: Key(8)] bool IsBot = false,
    [property: Key(9)] int? ClanId = null,
    [property: Key(10)] string? ClanName = null,
    [property: Key(11)] string? ClanAcronym = null);
