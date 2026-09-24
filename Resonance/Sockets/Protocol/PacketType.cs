namespace Resonance.Sockets.Protocol;

public enum PacketType : byte
{
    Profile = 1,
    OnlinePlayers = 2,
    PlayerJoined = 3,
    PlayerLeft = 4,
    Ping = 10,
    Pong = 12
}
