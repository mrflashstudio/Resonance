using Resonance.Models;

namespace Resonance.Services;

public interface IAuthenticationService
{
    Task<RegistrationResponse?> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthenticatedSession?> GetSessionAsync(string token, string client, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid id, CancellationToken cancellationToken);
    Task RevokeAsync(string token, string client, CancellationToken cancellationToken);
}
