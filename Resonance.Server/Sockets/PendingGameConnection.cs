namespace Resonance.Server.Sockets;

public sealed class PendingGameConnection : IDisposable
{
    private Action? _release;

    public PendingGameConnection(int accountId, Action release)
    {
        AccountId = accountId;
        _release = release;
    }

    public int AccountId { get; }

    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}
