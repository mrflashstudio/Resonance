namespace Resonance.Sockets;

public sealed class GameConnectionSettings
{
    public TimeSpan InactivityTimeout { get; init; } = TimeSpan.FromMinutes(2);
}
