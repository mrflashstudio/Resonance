namespace Resonance.Server.Sockets.Protocol;

public enum PacketType : byte
{
    Profile = 1,
    OnlinePlayers = 2,
    PlayerJoined = 3,
    PlayerLeft = 4,
    ChannelAvailable = 5,
    ChannelJoined = 6,
    JoinChannel = 7,
    ChannelLeft = 8,
    ChatMessage = 9,
    Ping = 10,
    PlayerActivity = 11,
    Pong = 12
}
