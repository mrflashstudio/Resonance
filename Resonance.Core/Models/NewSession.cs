namespace Resonance.Core.Models;

public sealed record NewSession(
    Guid Id,
    int UserId,
    string TokenHash,
    SessionClient Client,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
