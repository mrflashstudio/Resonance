using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Resonance.Core.Extensions;
using Resonance.Core.Models;

namespace Resonance.Core.Commands;

public sealed class BotCommandDispatcher : IDisposable
{
    private static readonly ChatCommandAttribute HelpMetadata = new("help")
    {
        Usage = "/help [command]",
        Description = "List available commands or show details for a command",
        Example = "/help roll"
    };

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
        return Find(GetName(text)) is { } command ? command.Metadata.InvocationVisibility : CommandVisibility.SenderOnly;
    }

    public bool IsCommand(string text)
    {
        var name = GetName(text);
        return name.Equals("/help", StringComparison.OrdinalIgnoreCase) || Find(name) is not null;
    }

    public async Task<CommandReply?> DispatchAsync(CommandContext context, string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var name = GetName(text);
        if (name.Length == 0 || name.Equals("/help", StringComparison.OrdinalIgnoreCase))
            return GetHelp(context.Sender, ReadArguments(text.TrimStart()[name.Length..]));

        var command = Find(name);
        if (command is null)
            return context.Channel is null ? new CommandReply("Unknown command. Use /help to see available commands.", CommandVisibility.SenderOnly) : null;
        
        if (!CanExecute(context.Sender, command.Value.Metadata))
            return new CommandReply("You do not have permission to use this command.", CommandVisibility.SenderOnly);

        var arguments = ReadArguments(text.TrimStart()[name.Length..]);
        var result = arguments is null ? new InvalidCommandArguments() :
            await command.Value.Handler.ExecuteAsync(context, arguments, cancellationToken);
        return result switch
        {
            InvalidCommandArguments => new CommandReply($"Usage: {command.Value.Metadata.Usage}", CommandVisibility.SenderOnly),
            CommandReply reply => reply with { Visibility = reply.Visibility ?? command.Value.Metadata.ReplyVisibility },
            _ => throw new InvalidOperationException("Unsupported command result.")
        };
    }

    private CommandReply GetHelp(Account sender, IReadOnlyList<string>? arguments)
    {
        if (arguments is null || arguments.Count > 1)
            return new CommandReply($"Usage: {HelpMetadata.Usage}", CommandVisibility.SenderOnly);

        if (arguments.Count == 0)
        {
            var names = _commands.Values.Select(command => command.Metadata)
                .Where(metadata => CanExecute(sender, metadata))
                .OrderBy(metadata => metadata.Name, StringComparer.Ordinal).Select(metadata => "/" + metadata.Name);
            return new CommandReply("Available commands: " + string.Join(", ", new[] { "/help" }.Concat(names)) +
                " | Use /help <command> for details", CommandVisibility.SenderOnly);
        }

        var name = arguments[0].StartsWith('/') ? arguments[0] : "/" + arguments[0];
        var metadata = name.Equals("/help", StringComparison.OrdinalIgnoreCase) ? HelpMetadata : Find(name)?.Metadata;
        if (metadata is null)
            return new CommandReply("Unknown command. Use /help to see available commands.", CommandVisibility.SenderOnly);
        if (!CanExecute(sender, metadata))
            return new CommandReply("You do not have permission to use this command.", CommandVisibility.SenderOnly);

        var lines = new List<string> { metadata.Usage };
        if (!string.IsNullOrWhiteSpace(metadata.Description))
            lines.Add(metadata.Description);
        if (!string.IsNullOrWhiteSpace(metadata.Example))
            lines.Add("Example: " + metadata.Example);
        return new CommandReply(string.Join(" | ", lines), CommandVisibility.SenderOnly);
    }

    private (IBotCommand Handler, ChatCommandAttribute Metadata)? Find(string name) =>
        name.StartsWith('/') && _commands.TryGetValue(name[1..], out var command) ? command : null;

    private static bool CanExecute(Account sender, ChatCommandAttribute metadata) =>
        metadata.RequiredPrivileges == UserPrivileges.None || sender.HasAnyPrivilege(metadata.RequiredPrivileges);

    private static string GetName(string text) =>
        text.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    private static string[]? ReadArguments(string text)
    {
        var arguments = new List<string>();
        for (var index = 0; index < text.Length;)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                continue;
            }

            var value = new StringBuilder();
            if (text[index] == '"')
            {
                index++;
                while (index < text.Length && text[index] != '"')
                {
                    if (text[index] == '\\' && index + 1 < text.Length && text[index + 1] is '"' or '\\')
                        index++;
                    value.Append(text[index++]);
                }

                if (index == text.Length)
                    return null;
                index++;
                if (index < text.Length && !char.IsWhiteSpace(text[index]))
                    return null;
            }
            else
            {
                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                {
                    if (text[index] == '"')
                        return null;
                    value.Append(text[index++]);
                }
            }

            arguments.Add(value.ToString());
        }

        return arguments.ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        foreach (var handler in _commands.Values.Select(command => command.Handler).OfType<IDisposable>())
            handler.Dispose();
    }
}
