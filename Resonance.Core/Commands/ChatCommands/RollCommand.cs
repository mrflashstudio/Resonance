using System.Globalization;

namespace Resonance.Core.Commands.ChatCommands;

[ChatCommand("roll", Usage = "/roll [max]", InvocationVisibility = CommandVisibility.Channel,
    ReplyVisibility = CommandVisibility.Channel, Description = "Roll from 1 to max inclusive (default 100)", Example = "/roll 20")]
public sealed class RollCommand : IBotCommand
{
    public Task<CommandResult> ExecuteAsync(CommandContext context, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var max = 100;
        if (arguments.Count > 1 || arguments.Count == 1 &&
            (!int.TryParse(arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out max) || max < 1))
            return Task.FromResult<CommandResult>(new InvalidCommandArguments());

        var result = Random.Shared.NextInt64(1, (long)max + 1);
        
        return Task.FromResult<CommandResult>(new CommandReply($"{context.Sender.Name} rolled {result} (1-{max})."));
    }
}
