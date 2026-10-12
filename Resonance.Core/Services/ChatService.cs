using Resonance.Core.Extensions;
using Resonance.Core.Models;

namespace Resonance.Core.Services;

public sealed class ChatService(IProfileService profiles, TimeProvider clock)
{
    public const int MaximumMessageLength = 500;

    public async Task<ChatMessage?> CreateMessageAsync(int senderId, int? recipientId, string? channel,
        string? text, CancellationToken cancellationToken)
    {
        if (text is null || text.Length > MaximumMessageLength ||
            recipientId is <= 0 || recipientId == senderId)
            return null;

        var hasRecipient = recipientId.HasValue;
        var hasChannel = !string.IsNullOrWhiteSpace(channel);
        if (hasRecipient == hasChannel)
            return null;

        text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (text.Length == 0 || text.Any(char.IsControl))
            return null;

        var sender = await profiles.GetAsync(senderId, cancellationToken);
        if (sender is null || sender.Status.GetEffectiveStatus(sender.StatusExpiresAt, clock.GetUtcNow()) != UserStatus.Active)
            return null;

        return new ChatMessage(sender, recipientId, channel?.Trim(), text)
        {
            Badges = sender.GetBadgeNames()
        };
    }
}
