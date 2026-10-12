using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Resonance.Core.Extensions;
using Resonance.Core.Models;

namespace Resonance.Core.Commands;

public sealed class BotCommandDispatcher : IDisposable
{
    private readonly Dictionary<string, (IBotCommand Handler, ChatCommandAttribute Metadata)> _commands = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public BotCommandDispatcher(IServiceProvider services, Assembly? commandAssembly = null)
    {
        var handlers = new List<IBotCommand>();
        try
        {
            var types = (commandAssembly ?? typeof(IBotCommand).Assembly).GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters &&
                    typeof(IBotCommand).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
            
            foreach (var type in types)
            {
                var metadata = type.GetCustomAttribute<ChatCommandAttribute>();
                if (metadata is null)
                    continue;
                
                if (string.IsNullOrWhiteSpace(metadata.Name) || metadata.Name.Any(char.IsWhiteSpace) || metadata.Name.Contains('/') ||
                    metadata.Name.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                    !Enum.IsDefined(metadata.InvocationVisibility) || !Enum.IsDefined(metadata.ReplyVisibility))
                    throw new InvalidOperationException($"Invalid command metadata on {type.Name}.");

                var handler = (IBotCommand)ActivatorUtilities.CreateInstance(services, type);
                handlers.Add(handler);
                _commands.Add(metadata.Name, (handler, metadata));
            }
        }
        catch
        {
            foreach (var handler in handlers.OfType<IDisposable>())
                handler.Dispose();
            
            throw;
        }
    }

    public T GetCommand<T>() where T : IBotCommand => _commands.Values.Select(command => command.Handler).OfType<T>().Single();

    public CommandVisibility GetInvocationVisibility(string text)
    {
        var words = Split(text);
        return words.Length > 0 && Find(words[0]) is { } command ? command.Metadata.InvocationVisibility : CommandVisibility.SenderOnly;
    }

    public bool IsCommand(string text)
    {
        var words = Split(text);
        return words.Length > 0 && (words[0].Equals("/help", StringComparison.OrdinalIgnoreCase) || Find(words[0]) is not null);
    }

    public async Task<CommandReply?> DispatchAsync(CommandContext context, string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var words = Split(text);
        if (words.Length == 0 || words[0].Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            var usages = _commands.Values.Select(command => command.Metadata)
                .Where(metadata => CanExecute(context.Sender, metadata))
                .OrderBy(metadata => metadata.Name, StringComparer.Ordinal).Select(metadata => metadata.Usage);
            return new CommandReply("Available commands: " + string.Join(", ", new[] { "/help" }.Concat(usages)), CommandVisibility.SenderOnly);
        }

        var command = Find(words[0]);
        if (command is null)
            return context.Channel is null ? new CommandReply("Unknown command. Use /help to see available commands.", CommandVisibility.SenderOnly) : null;
        
        if (!CanExecute(context.Sender, command.Value.Metadata))
            return new CommandReply("You do not have permission to use this command.", CommandVisibility.SenderOnly);

        var result = await command.Value.Handler.ExecuteAsync(context, words[1..], cancellationToken);
        return result switch
        {
            InvalidCommandArguments => new CommandReply($"Usage: {command.Value.Metadata.Usage}", CommandVisibility.SenderOnly),
            CommandReply reply => reply with { Visibility = reply.Visibility ?? command.Value.Metadata.ReplyVisibility },
            _ => throw new InvalidOperationException("Unsupported command result.")
        };
    }

    private (IBotCommand Handler, ChatCommandAttribute Metadata)? Find(string name) =>
        name.StartsWith('/') && _commands.TryGetValue(name[1..], out var command) ? command : null;

    private static bool CanExecute(Account sender, ChatCommandAttribute metadata) =>
        metadata.RequiredPrivileges == UserPrivileges.None || sender.HasAnyPrivilege(metadata.RequiredPrivileges);

    private static string[] Split(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        foreach (var handler in _commands.Values.Select(command => command.Handler).OfType<IDisposable>())
            handler.Dispose();
    }
}
