namespace Resonance.Core.Commands;

public abstract record CommandResult;

public sealed record InvalidCommandArguments : CommandResult;
