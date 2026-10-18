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
    Pong = 12,
    UserSilence = 13,
    UserModerationChanged = 14,
    StartSpectating = 15,
    StopSpectating = 16,
    SpectatorFrames = 17,
    SpectatorJoined = 18,
    SpectatorLeft = 19,
    Unknown = 20, // doesn't appear to exist or be handled by the game
    SpectatingEnded = 21,
    SpectatorRedirected = 22,
    MultiplayerLobbyCommand = 23,
    MultiplayerLobbyState = 24,
    MultiplayerLobbyCommandResult = 25,
    SubmitScore = 26,
    ScoreSubmissionResult = 27,
    RequestMultiplayerLobbies = 28,
    MultiplayerLobbies = 29,
    MultiplayerLobbyAdded = 30,
    MultiplayerLobbyRemoved = 31,
    MultiplayerLobbyUpdated = 32
}
