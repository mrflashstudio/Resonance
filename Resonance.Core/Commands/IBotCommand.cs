namespace Resonance.Core.Commands;

public interface IBotCommand
{
    Task<CommandResult> ExecuteAsync(CommandContext context, IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}
