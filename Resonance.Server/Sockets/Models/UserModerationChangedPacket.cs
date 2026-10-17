using MessagePack;

namespace Resonance.Server.Sockets.Models;

[MessagePackObject]
public sealed record UserModerationChangedPacket
{
    [Key(0)] public int UserId { get; init; }
    [Key(1)] public bool IsLimited { get; init; }
    [Key(2)] public string? Status { get; init; }
    [Key(3)] public long? ExpiresAt { get; init; }
}
