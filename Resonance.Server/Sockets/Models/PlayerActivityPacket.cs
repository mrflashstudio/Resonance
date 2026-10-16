using MessagePack;

namespace Resonance.Server.Sockets.Models;

[MessagePackObject]
public sealed record PlayerActivityPacket
{
    [Key(0)] public int UserId { get; init; }
    [Key(1)] public PlayerActivity? Activity { get; init; }
    [Key(2)] public string? MapTitle { get; init; }
    [Key(3)] public int? MapId { get; init; }
    [Key(8)] public int SpectatorCount { get; init; }
    [Key(9)] public string[]? SpectatorNames { get; init; }
    [Key(10)] public string? ReplayUserName { get; init; }
}
