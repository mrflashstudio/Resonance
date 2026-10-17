using Resonance.Core.Models;

namespace Resonance.Core.Repositories;

public interface IUserRepository
{
    Task<Account?> InitializeServerBotAsync(string name, DateTimeOffset createdAt, CancellationToken cancellationToken);
    Task<bool> HasSuperuserAsync(CancellationToken cancellationToken);
    Task<bool> TryClaimSuperuserAsync(int userId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<Account?> GetAsync(int id, CancellationToken cancellationToken);
    Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> ChangePrivilegesAsync(int id, UserPrivileges privileges, bool grant, CancellationToken cancellationToken);
    Task<bool> ChangeBadgesAsync(int id, UserBadges badges, bool grant, CancellationToken cancellationToken);
    Task<Account?> ChangeSilenceAsync(int id, DateTimeOffset? silencedAt, DateTimeOffset? expiresAt, string? reason,
        CancellationToken cancellationToken);
    Task<UserCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<RegistrationResult?> CreateAsync(string username, string passwordHash, DateTimeOffset createdAt,
        CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task RecordActivityAsync(int id, DateTimeOffset observedAt, CancellationToken cancellationToken);
}
