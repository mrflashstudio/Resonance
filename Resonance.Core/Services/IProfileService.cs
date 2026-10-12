using Resonance.Core.Models;

namespace Resonance.Core.Services;

public interface IProfileService
{
    Task<Account?> GetAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
}
