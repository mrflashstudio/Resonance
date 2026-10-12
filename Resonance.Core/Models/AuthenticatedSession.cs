namespace Resonance.Core.Models;

public sealed record AuthenticatedSession(Guid Id, int UserId, DateTimeOffset ExpiresAt);
