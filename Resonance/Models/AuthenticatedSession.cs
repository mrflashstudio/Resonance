namespace Resonance.Models;

public sealed record AuthenticatedSession(Guid Id, Account Account, DateTimeOffset ExpiresAt);
