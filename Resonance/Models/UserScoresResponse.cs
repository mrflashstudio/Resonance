using System.Text.Json;

namespace Resonance.Models;

public sealed record UserScoresResponse(string? Error = null)
{
    public JsonElement[] Top { get; init; } = [];
    public JsonElement[] LastDay { get; init; } = [];
    public JsonElement[] Reign { get; init; } = [];
}
