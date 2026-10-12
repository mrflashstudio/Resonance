using Resonance.Core.Models;

namespace Resonance.Core.Services;

public interface IAuthenticationService
{
    Task<RegistrationOutcome> RegisterAsync(string username, string password, CancellationToken cancellationToken);
    Task<LoginResult> LoginAsync(string login, string passwordProof, SessionClient client, CancellationToken cancellationToken);
    Task<AuthenticatedSession?> GetSessionAsync(string token, SessionClient client, CancellationToken cancellationToken);
    Task<AuthenticatedSession?> GetSessionForLogoutAsync(string token, SessionClient client, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid id, CancellationToken cancellationToken);
    DateTimeOffset? GetSessionExpiry(Guid id);
    Task RevokeAsync(string token, SessionClient client, CancellationToken cancellationToken);
    Task RevokeUserSessionsAsync(int userId, CancellationToken cancellationToken);
    Task RemoveExpiredSessionsAsync(CancellationToken cancellationToken);
}
