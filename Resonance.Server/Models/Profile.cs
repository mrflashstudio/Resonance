using System.Text.Json.Serialization;
using MessagePack;

namespace Resonance.Server.Models;

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
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    [Key(4)]
    [JsonPropertyName("profile_image")]
    public string? ProfileImage { get; init; }

    [Key(5)]
    [JsonPropertyName("about_me")]
    public string? AboutMe { get; init; }

    [Key(6)]
    [JsonPropertyName("created_at")]
    public double? CreatedAt { get; init; }

    [Key(7)]
    public string? Flag { get; init; }

    [Key(8)]
    public int? Position { get; init; }

    [Key(9)]
    [JsonPropertyName("skill_points")]
    public double? SkillPoints { get; init; }

    [Key(10)]
    [JsonPropertyName("play_count")]
    public int? PlayCount { get; init; }

    [Key(11)]
    [JsonPropertyName("squares_hit")]
    public int? SquaresHit { get; init; }

    [Key(12)]
    [JsonPropertyName("total_score")]
    public long? TotalScore { get; init; }

    [Key(13)]
    [JsonPropertyName("is_online")]
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

    [Key(19)]
    public string[]? Achievements { get; init; }
}
