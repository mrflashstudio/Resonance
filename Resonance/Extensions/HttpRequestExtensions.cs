using Resonance.Routing;

namespace Resonance.Extensions;

public static class HttpRequestExtensions
{
    public static bool HasTrustedOrigin(this HttpRequest request, ServerOptions server)
    {
        if (!Uri.TryCreate(request.Headers.Origin.ToString(), UriKind.Absolute, out var origin) ||
            origin.Scheme != Uri.UriSchemeHttps || !origin.IsDefaultPort || origin.AbsolutePath != "/" ||
            origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            return false;

        return string.Equals(origin.Host, server.Domain, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(origin.Host, $"www.{server.Domain}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(origin.Host, $"production.{server.Domain}", StringComparison.OrdinalIgnoreCase);
    }
}
