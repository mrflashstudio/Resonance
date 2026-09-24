using MessagePack;

namespace Resonance.Sockets.Models;

[MessagePackObject]
public sealed record Profile
{
    [Key(0)]
    public required int Id { get; init; }

    [Key(1)]
    public required string Uid { get; init; }

    [Key(2)]
    public required string Username { get; init; }

    [Key(3)]
    public string? AvatarUrl { get; init; }

    [Key(4)]
    public string? ProfileImage { get; init; }

    [Key(5)]
    public string? AboutMe { get; init; }

    [Key(6)]
    public double? CreatedAt { get; init; }

    [Key(7)]
    public string? Flag { get; init; }

    [Key(8)]
    public int? Position { get; init; }

    [Key(9)]
    public double? SkillPoints { get; init; }

    [Key(10)]
    public int? PlayCount { get; init; }

    [Key(11)]
    public int? SquaresHit { get; init; }

    [Key(12)]
    public long? TotalScore { get; init; }

    [Key(13)]
    public bool? IsOnline { get; init; }

    [Key(14)]
    public bool? Verified { get; init; }

    [Key(15)]
    public string? Ban { get; init; }

    [Key(16)]
    public string[]? Badges { get; init; }

    [Key(17)]
    public ClanSummary? Clans { get; init; }

    [Key(18)]
    public int? Clan { get; init; }

}
