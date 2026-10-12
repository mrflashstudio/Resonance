namespace Resonance.Core.Configuration;

public sealed class RateLimitSettings
{
    public int AuthenticationConcurrency { get; init; } = 5;
    public int HttpRequestsPerSecond { get; init; } = 20;
    public int GamePacketsPerSecond { get; init; } = 50;
}
