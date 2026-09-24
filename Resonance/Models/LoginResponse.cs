namespace Resonance.Models;

public sealed record LoginResponse(
    string? Error = null,
    string? Session = null,
    int ProfileId = 0,
    bool VerificationRequired = false,
    string? VerificationPollToken = null);
