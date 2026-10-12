using MessagePack;

namespace Resonance.Server.Sockets.Models;

[MessagePackObject]
public sealed record ChatChannelPacket([property: Key(0)] string? Name);
