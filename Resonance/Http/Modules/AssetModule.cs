using System.Security.Cryptography;
using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Resonance.Assets;
using Resonance.Configuration;
using Resonance.Core.Models;
using Resonance.Core.Services;

namespace Resonance.Http.Modules;

public sealed class AssetModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        var domain = endpoints.ServiceProvider.GetRequiredService<IOptions<ServerOptions>>().Value.Domain;
        var assets = endpoints.MapGroup("").RequireHost($"static.{domain}");

        assets.MapMethods("/avatars/{id:int}", ["GET", "HEAD"], GetAvatarAsync);
        assets.MapMethods("/banners/{id:int}", ["GET", "HEAD"], GetBannerAsync);
    }

    private static Task<Results<FileStreamHttpResult, NotFound>> GetAvatarAsync(
        int id, HttpContext context, IProfileService profiles, UserAssets assets, CancellationToken cancellationToken)
    {
        return GetAsync(id, context, profiles, assets, AssetKind.Avatar, cancellationToken);
    }

    private static Task<Results<FileStreamHttpResult, NotFound>> GetBannerAsync(
        int id, HttpContext context, IProfileService profiles, UserAssets assets, CancellationToken cancellationToken)
    {
        return GetAsync(id, context, profiles, assets, AssetKind.Banner, cancellationToken);
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> GetAsync(
        int id, HttpContext context, IProfileService profiles, UserAssets assets, AssetKind kind, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";

        if (id <= 0 || await profiles.GetAsync(id, cancellationToken) is not { } account)
            return TypedResults.NotFound();

        var file = assets.Find(account, kind);

        if (file is null)
            return TypedResults.NotFound();

        FileStream stream;

        try
        {
            stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read,
                FileShare.Read | FileShare.Delete, bufferSize: 65536, useAsync: true);
        }
        catch (IOException exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            var entityTag = new EntityTagHeaderValue($"\"{Convert.ToHexString(hash)}\"");

            stream.Position = 0;

            context.Response.Headers.CacheControl = "public, no-cache";
            context.Response.Headers.XContentTypeOptions = "nosniff";

            return TypedResults.File(stream, "image/png", entityTag: entityTag);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }
}
