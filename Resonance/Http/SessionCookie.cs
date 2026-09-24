using Resonance.Routing;

namespace Resonance.Http;

public static class SessionCookie
{
    public const string Name = "__Host-ResonanceSession";

    public static CookieOptions CreateOptions(TimeSpan? lifetime = null)
    {
        return new CookieOptions
        {
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = lifetime,
            IsEssential = true
        };
    }
}
