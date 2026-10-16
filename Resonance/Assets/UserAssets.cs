using Microsoft.Extensions.Options;
using Resonance.Configuration;
using Resonance.Core.Models;
using Resonance.Core.Services;

namespace Resonance.Assets;

public sealed class UserAssets(
    IWebHostEnvironment environment,
    IOptions<AssetSettings> settings,
    IOptions<ServerOptions> server) : IProfileAssets
{
    private readonly string _customPath = Path.GetFullPath(settings.Value.Path, environment.ContentRootPath);
    private readonly string _defaultPath = Path.Combine(environment.ContentRootPath, "Assets", "defaults");

    public string? GetUrl(Account account, AssetKind kind)
    {
        if (Find(account, kind) is null)
            return null;

        return $"https://static.{server.Value.Domain}/{GetDirectory(kind)}/{account.Id}";
    }

    public FileInfo? Find(Account account, AssetKind kind)
    {
        var directory = GetDirectory(kind);
        var custom = new FileInfo(Path.Combine(_customPath, directory, $"{account.Id}.png"));

        if (custom.Exists)
            return custom;

        var owner = account.IsBot ? "bot" : "user";
        var fallback = new FileInfo(Path.Combine(_defaultPath, directory, owner + ".png"));

        return fallback.Exists ? fallback : null;
    }

    private static string GetDirectory(AssetKind kind)
    {
        return kind switch
        {
            AssetKind.Avatar => "avatars",
            AssetKind.Banner => "banners",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}
