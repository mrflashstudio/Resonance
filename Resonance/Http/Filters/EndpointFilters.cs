using Microsoft.AspNetCore.Http.Features;

namespace Resonance.Http.Filters;

public static class EndpointFilters
{
    internal static void DisableCaching(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store,no-cache";
        response.Headers.Pragma = "no-cache";
    }

    internal static async ValueTask<object?> NoStoreAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        DisableCaching(context.HttpContext.Response);

        return await next(context);
    }

    internal static async ValueTask<object?> LimitBodyAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (feature is { IsReadOnly: false })
            feature.MaxRequestBodySize = 4096;

        try
        {
            return await next(context);
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            context.HttpContext.Response.Headers.Remove("Cache-Control");
            context.HttpContext.Response.Headers.Remove("Pragma");

            return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }
    }
}
