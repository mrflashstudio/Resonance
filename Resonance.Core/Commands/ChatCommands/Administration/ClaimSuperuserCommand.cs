using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Resonance.Core.Repositories;

namespace Resonance.Core.Commands.ChatCommands.Administration;

[ChatCommand("claimsuperuser", Usage = "/claimsuperuser <code>",
    Description = "Claim full access using the single-use code from the server logs, while no superuser exists",
    Example = "/claimsuperuser your-code")]
public sealed class ClaimSuperuserCommand(IUserRepository users, TimeProvider clock) : IBotCommand, IDisposable
{
    private readonly SemaphoreSlim _claims = new(1, 1);
    private byte[]? _codeHash;
    private bool _initialized;

    public async Task<string?> InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            throw new InvalidOperationException("Superuser claiming is already initialized.");
        
        _initialized = true;
        
        if (await users.HasSuperuserAsync(cancellationToken))
            return null;

        var code = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        _codeHash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        
        return code;
    }

    public async Task<CommandResult> ExecuteAsync(CommandContext context, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        if (arguments.Count != 1)
            return new InvalidCommandArguments();

        var code = arguments[0];
        await _claims.WaitAsync(cancellationToken);
        
        try
        {
            if (_codeHash is null || code.Length != 43 ||
                !CryptographicOperations.FixedTimeEquals(_codeHash, SHA256.HashData(Encoding.UTF8.GetBytes(code))))
                return new CommandReply("The claim code is invalid or no longer available.");

            _codeHash = null;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10), clock);
            return new CommandReply(await users.TryClaimSuperuserAsync(context.Sender.Id, clock.GetUtcNow(), timeout.Token)
                ? "You are now a superuser."
                : "Superuser claiming is no longer available. Restart the server if no superuser exists.");
        }
        finally
        {
            _claims.Release();
        }
    }

    public void Dispose() => _claims.Dispose();
}
