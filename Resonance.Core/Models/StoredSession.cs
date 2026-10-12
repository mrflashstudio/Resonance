namespace Resonance.Core.Models;

public sealed record StoredSession(Guid Id, int UserId, string TokenHash, SessionClient Client, DateTimeOffset ExpiresAt);
