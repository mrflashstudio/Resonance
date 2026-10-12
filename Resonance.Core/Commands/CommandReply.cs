namespace Resonance.Core.Commands;

public sealed record CommandReply(string Text, CommandVisibility? Visibility = null) : CommandResult;
