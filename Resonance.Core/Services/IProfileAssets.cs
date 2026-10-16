using Resonance.Core.Models;

namespace Resonance.Core.Services;

public interface IProfileAssets
{
    string? GetUrl(Account account, AssetKind kind);
}
