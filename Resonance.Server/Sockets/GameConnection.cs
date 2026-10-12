using System.Threading.Channels;

namespace Resonance.Server.Sockets;

public sealed class GameConnection(int accountId, Guid sessionId) : IDisposable
{
    private readonly Channel<byte[]> _outgoing = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(256)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.Wait,
        AllowSynchronousContinuations = false
    });
    private readonly CancellationTokenSource _stopped = new();

    public int AccountId { get; } = accountId;
    public Guid SessionId { get; } = sessionId;
    public CancellationToken Stopped => _stopped.Token;
    public ChannelReader<byte[]> Outgoing => _outgoing.Reader;

    public void Send(byte[] packet)
    {
        if (!_outgoing.Writer.TryWrite(packet))
            _stopped.Cancel();
    }

    public void Dispose()
    {
        _outgoing.Writer.TryComplete();
        _stopped.Dispose();
    }

    public void Stop()
    {
        _stopped.Cancel();
    }
}
