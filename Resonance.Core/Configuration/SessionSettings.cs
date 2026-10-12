namespace Resonance.Core.Configuration;

public sealed class SessionSettings
{
    public static readonly TimeSpan RenewalInterval = TimeSpan.FromDays(1);
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromDays(1);
}
