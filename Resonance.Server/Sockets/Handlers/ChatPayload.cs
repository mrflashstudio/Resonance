using MessagePack;

namespace Resonance.Server.Sockets.Handlers;

internal static class ChatPayload
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    public static T? Read<T>(ReadOnlyMemory<byte> payload) where T : class
    {
        if (payload.IsEmpty || payload.Length > 4096)
            return null;

        try
        {
            var framing = new MessagePackReader(payload);
            framing.Skip();
            if (!framing.End)
                return null;
            var reader = new MessagePackReader(payload);
            var result = MessagePackSerializer.Deserialize<T>(ref reader, Options);
            return reader.End ? result : null;
        }
        catch (MessagePackSerializationException)
        {
            return null;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }
}
