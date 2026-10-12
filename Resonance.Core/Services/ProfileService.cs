using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Services;

public sealed class ProfileService(IUserRepository users, IPresence presence) : IProfileService
{
    public async Task<Account?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var account = await users.GetAsync(id, cancellationToken);

        return account is null ? null : account with { IsOnline = presence.IsOnline(id) };
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return users.ExistsAsync(id, cancellationToken);
    }
}
