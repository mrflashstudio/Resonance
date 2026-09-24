using Resonance.Sockets;

namespace Resonance.Services;

public interface IOnlinePlayers
{
    int Count { get; }
    bool IsOnline(int userId);
    void Connect(GameConnection connection);
    bool Disconnect(GameConnection connection);
    void DisconnectSession(Guid sessionId);
    void SendSnapshot(GameConnection connection);
}
