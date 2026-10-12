namespace Resonance.Server.Configuration;

public sealed class GameConnectionSettings
{
    public TimeSpan InactivityTimeout { get; init; } = TimeSpan.FromMinutes(2);
}
