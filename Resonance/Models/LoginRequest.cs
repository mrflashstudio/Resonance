namespace Resonance.Models;

public sealed record LoginRequest(string? Login, string? PasswordProof, string? Client);
