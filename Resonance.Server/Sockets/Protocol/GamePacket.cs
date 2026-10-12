using System.Buffers.Binary;

namespace Resonance.Server.Sockets.Protocol;

public static class GamePacket
{
    public const int HeaderSize = 2;
    public const int PayloadHeaderSize = 6;

    public static byte[] Create(PacketType type, ReadOnlySpan<byte> payload = default)
    {
        if (payload.IsEmpty)
            return [0, (byte)type];

        var packet = new byte[PayloadHeaderSize + payload.Length];
        packet[0] = 1;
        packet[1] = (byte)type;
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(HeaderSize), payload.Length);
        payload.CopyTo(packet.AsSpan(PayloadHeaderSize));

        return packet;
    }

    public static bool TryRead(ReadOnlyMemory<byte> packet, out PacketType type, out ReadOnlyMemory<byte> payload)
    {
        type = default;
        payload = default;
        var bytes = packet.Span;

        if (packet.Length < HeaderSize)
            return false;

        if (bytes[0] == 0 && packet.Length != HeaderSize)
            return false;

        if (bytes[0] == 1 && (packet.Length < PayloadHeaderSize ||
            BinaryPrimitives.ReadInt32LittleEndian(bytes[HeaderSize..]) != packet.Length - PayloadHeaderSize))
            return false;

        if (bytes[0] > 1)
            return false;

        type = (PacketType)bytes[1];
        payload = bytes[0] == 1 ? packet[PayloadHeaderSize..] : default;

        return true;
    }
}
