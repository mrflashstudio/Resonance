using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Resonance.Routing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class WebSocketRequestAttribute(bool required) : Attribute, IActionConstraint
{
    public int Order => 0;

    public bool Accept(ActionConstraintContext context)
    {
        return context.RouteContext.HttpContext.WebSockets.IsWebSocketRequest == required;
    }
}
