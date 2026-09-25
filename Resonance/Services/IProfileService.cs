using Resonance.Models;

namespace Resonance.Services;

public interface IProfileService
{
    Task<Profile?> GetAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
}
