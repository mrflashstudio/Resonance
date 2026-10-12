using Resonance.Core.Models;

namespace Resonance.Core.Commands;

public sealed record CommandContext(Account Sender, string? Channel);
