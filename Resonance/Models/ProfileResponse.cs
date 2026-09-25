namespace Resonance.Models;

public sealed record ProfileResponse(string? Error = null, Profile? User = null);
