using Resonance.Core.Models;

namespace Resonance.Core.Commands;

public sealed record BotReply(ChatMessage Message, CommandVisibility Visibility);
