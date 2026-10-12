namespace Resonance.Core.Models;

public sealed record ChatMessage(int SenderId, string SenderName, int? RecipientId, string? Channel, string Text)
{
    public string[] Badges { get; init; } = [];
}
