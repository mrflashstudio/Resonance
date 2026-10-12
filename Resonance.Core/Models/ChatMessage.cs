namespace Resonance.Core.Models;

public sealed record ChatMessage(Account Sender, int? RecipientId, string? Channel, string Text)
{
    public int SenderId => Sender.Id;
    public string SenderName => Sender.Name;
    public string[] Badges { get; init; } = [];
}
