namespace Resonance.Core.Services;

public interface IPresence
{
    int Count { get; }
    bool IsOnline(int userId);
    void DisconnectSession(Guid sessionId);
}
