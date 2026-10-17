using MessagePack;

namespace Resonance.Server.Sockets.Models;

[MessagePackObject]
public sealed record UserSilencePacket
{
    [Key(0)] public int UserId { get; init; }
    [Key(1)] public bool Silenced { get; init; }
    [Key(2)] public long? ExpiresAt { get; init; }
    [Key(3)] public string? Reason { get; init; }
}
