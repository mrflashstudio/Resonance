using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.Options;

namespace Resonance.Routing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SubdomainAttribute(params string[] subdomains) : Attribute, IActionConstraint
{
    public int Order => 0;

    public bool Accept(ActionConstraintContext context)
    {
        var httpContext = context.RouteContext.HttpContext;
        var domain = httpContext.RequestServices.GetRequiredService<IOptions<ServerOptions>>().Value.Domain;
        var host = httpContext.Request.Host.Host;

        return subdomains.Any(subdomain =>
            string.Equals(host, $"{subdomain}.{domain}", StringComparison.OrdinalIgnoreCase));
    }
}
