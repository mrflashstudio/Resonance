using MessagePack;

namespace Resonance.Server.Sockets.Models;

[MessagePackObject]
public sealed record ChatMessagePacket
{
    [Key(0)] public int SenderId { get; init; }
    [Key(1)] public int? RecipientId { get; init; }
    [Key(2)] public string? Channel { get; init; }
    [Key(3)] public string? Text { get; init; }
    [Key(4)] public string? SenderName { get; init; }
    [Key(5)] public bool SuppressNotification { get; init; }
    [Key(6)] public string[]? Badges { get; init; }
}
