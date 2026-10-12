using Resonance.Core.Models;

namespace Resonance.Core.Repositories;

public interface IUserRepository
{
    Task<Account?> GetAsync(int id, CancellationToken cancellationToken);
    Task<UserCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<RegistrationResult?> CreateAsync(string username, string passwordHash, DateTimeOffset createdAt,
        CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task RecordActivityAsync(int id, DateTimeOffset observedAt, CancellationToken cancellationToken);
}
