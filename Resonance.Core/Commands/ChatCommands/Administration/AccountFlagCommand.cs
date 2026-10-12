using System.Globalization;
using Resonance.Core.Repositories;

namespace Resonance.Core.Commands.ChatCommands.Administration;

public abstract class AccountFlagCommand<TFlags>(IUserRepository users, string action) : IBotCommand where TFlags : struct, Enum
{
    protected IUserRepository Users { get; } = users;

    protected abstract Task<bool> ChangeAsync(int id, TFlags flags, CancellationToken cancellationToken);

    public async Task<CommandResult> ExecuteAsync(CommandContext context, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        if (arguments.Count < 2)
            return new InvalidCommandArguments();

        var knownFlags = Enum.GetValues<TFlags>().Aggregate(0L, (mask, flag) => mask | Convert.ToInt64(flag));
        var parsed = Enum.TryParse<TFlags>(arguments[^1], true, out var flags);
        var bits = Convert.ToInt64(flags);
        if (!parsed || bits <= 0 || (bits & ~knownFlags) != 0)
            return new CommandReply($"Invalid value. Available names: {string.Join(", ", Enum.GetNames<TFlags>().Where(name => name != "None"))}.");

        var target = string.Join(" ", arguments.Take(arguments.Count - 1));
        var account = int.TryParse(target, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? await Users.GetAsync(id, cancellationToken)
            : await Users.GetByUsernameAsync(target, cancellationToken);
        if (account is null || !await ChangeAsync(account.Id, flags, cancellationToken))
            return new CommandReply("User not found.");

        return new CommandReply($"{action} {flags} for {account.Name} (ID {account.Id}).");
    }
}
