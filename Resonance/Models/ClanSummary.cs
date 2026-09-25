using MessagePack;

namespace Resonance.Models;

[MessagePackObject]
public sealed record ClanSummary(
    [property: Key(0)] int Id,
    [property: Key(1)] string Acronym,
    [property: Key(2)] string? Name);
