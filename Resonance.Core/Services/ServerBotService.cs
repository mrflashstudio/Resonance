using Microsoft.Extensions.Logging;
using Resonance.Core.Commands;
using Resonance.Core.Commands.ChatCommands.Administration;
using Resonance.Core.Extensions;
using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Services;

public sealed class ServerBotService(IUserRepository users, TimeProvider clock, BotCommandDispatcher commands,
    ILogger<ServerBotService> logger)
{
    private bool _initialized;
    public Account? Account { get; private set; }

    public async Task<Account?> InitializeAsync(string name, CancellationToken cancellationToken)
    {
        if (_initialized)
            throw new InvalidOperationException("Server bot is already initialized.");
        
        _initialized = true;

        Account = await users.InitializeServerBotAsync(name, clock.GetUtcNow(), cancellationToken);
        if (Account is null)
        {
            logger.LogWarning("Server bot was not created: the first account is already a human account");
            return null;
        }
        
        if (!Account.IsBot)
            throw new InvalidOperationException("Server bot must be a bot account.");

        logger.LogInformation("Server bot {Name} initialized (account {AccountId})", Account.Name, Account.Id);
        
        var code = await commands.GetCommand<ClaimSuperuserCommand>().InitializeAsync(cancellationToken);
        if (code is not null)
            logger.LogWarning("No superuser exists. Send /claimsuperuser {ClaimCode} to {BotName} or in a joined chat channel. This code is single-use and changes on restart",
                code, Account.Name);
        
        return Account;
    }

    public bool CanHandle(ChatMessage message) => Account is { } bot &&
        (message.RecipientId == bot.Id || (message.Channel is not null && commands.IsCommand(message.Text)));

    public CommandVisibility GetInvocationVisibility(ChatMessage message) => commands.GetInvocationVisibility(message.Text);

    public async Task<BotReply?> ReplyAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        if (Account is null || !CanHandle(message) ||
            message.Sender.IsBot || message.Sender.Status.GetEffectiveStatus(message.Sender.StatusExpiresAt, clock.GetUtcNow()) != UserStatus.Active)
            return null;

        var reply = await commands.DispatchAsync(new CommandContext(message.Sender, message.Channel), message.Text, cancellationToken);
        if (reply is null)
            return null;
        
        var response = new ChatMessage(Account, message.Channel is null ? message.SenderId : null, message.Channel, reply.Text)
        {
            Badges = Account.GetBadgeNames()
        };
        return new BotReply(response, reply.Visibility ?? CommandVisibility.SenderOnly);
    }
}
