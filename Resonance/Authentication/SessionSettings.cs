namespace Resonance.Authentication;

public sealed class SessionSettings
{
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(1);
}
