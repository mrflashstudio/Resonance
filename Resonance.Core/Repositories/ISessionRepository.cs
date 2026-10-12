using Resonance.Core.Models;

namespace Resonance.Core.Repositories;

public interface ISessionRepository
{
    Task CreateAsync(NewSession session, string? passwordHash, CancellationToken cancellationToken);
    Task<StoredSession?> FindActiveAsync(string tokenHash, SessionClient client, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<StoredSession?> FindActiveAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> RenewAsync(Guid id, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<Guid?> DeleteAsync(string tokenHash, SessionClient client, CancellationToken cancellationToken);
    Task<Guid[]> DeleteUserAsync(int userId, CancellationToken cancellationToken);
    Task DeleteInvalidAsync(string tokenHash, SessionClient client, DateTimeOffset now, CancellationToken cancellationToken);
    Task DeleteInvalidAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken);
    Task RemoveExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
