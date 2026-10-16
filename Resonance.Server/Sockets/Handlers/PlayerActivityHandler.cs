using Resonance.Server.Services;
using Resonance.Server.Sockets.Models;
using Resonance.Server.Sockets.Protocol;

namespace Resonance.Server.Sockets.Handlers;

public sealed class PlayerActivityHandler(OnlinePlayers onlinePlayers) : IPacketHandler
{
    public PacketType Type => PacketType.PlayerActivity;

    public ValueTask HandleAsync(GameConnection connection, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var request = PacketPayload.Read<PlayerActivityPacket>(payload);
        if (request is null || (request.Activity is { } activity && !Enum.IsDefined(activity)) ||
            request.MapId is < 0 || !IsValidText(request.MapTitle, 500) ||
            !IsValidText(request.ReplayUserName, 100))
            return ValueTask.CompletedTask;

        cancellationToken.ThrowIfCancellationRequested();

        onlinePlayers.UpdateActivity(connection, request);
        
        return ValueTask.CompletedTask;
    }

    private static bool IsValidText(string? value, int maximumLength)
    {
        return value is null || (value.Length <= maximumLength && !value.Any(char.IsControl));
    }
}
