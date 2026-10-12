namespace Resonance.Core.Models;

public sealed record LoginResult
{
    private LoginResult(string? error, string? session, int accountId)
    {
        Error = error;
        Session = session;
        AccountId = accountId;
    }

    public string? Error { get; }
    public string? Session { get; }
    public int AccountId { get; }

    public static LoginResult Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return new LoginResult(error, null, 0);
    }

    public static LoginResult Success(string session, int accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(session);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountId);

        return new LoginResult(null, session, accountId);
    }
}
