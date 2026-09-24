using System.Buffers.Binary;

namespace Resonance.Sockets.Protocol;

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

    public static bool TryRead(ReadOnlySpan<byte> packet, out PacketType type)
    {
        type = default;

        if (packet.Length < HeaderSize)
            return false;

        if (packet[0] == 0 && packet.Length != HeaderSize)
            return false;

        if (packet[0] == 1 && (packet.Length < PayloadHeaderSize ||
            BinaryPrimitives.ReadInt32LittleEndian(packet[HeaderSize..]) != packet.Length - PayloadHeaderSize))
            return false;

        if (packet[0] > 1)
            return false;

        type = (PacketType)packet[1];

        return true;
    }
}
