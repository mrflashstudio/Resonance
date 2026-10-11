using Microsoft.Extensions.Options;
using Resonance.Http.Filters;
using Resonance.Routing;

namespace Resonance.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapProductionApi(this IEndpointRouteBuilder endpoints)
    {
        var domain = endpoints.ServiceProvider.GetRequiredService<IOptions<ServerOptions>>().Value.Domain;

        return endpoints.MapGroup("/api")
            .RequireHost($"production.{domain}")
            .AddEndpointFilter(EndpointFilters.LimitBodyAsync);
    }
}
