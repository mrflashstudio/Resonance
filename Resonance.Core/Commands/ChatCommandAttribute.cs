using Resonance.Core.Models;

namespace Resonance.Core.Commands;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ChatCommandAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string Usage { get; set; } = "/" + name;
    public string? Description { get; set; }
    public string? Example { get; set; }
    public UserPrivileges RequiredPrivileges { get; set; } = UserPrivileges.None;
    public CommandVisibility InvocationVisibility { get; set; } = CommandVisibility.SenderOnly;
    public CommandVisibility ReplyVisibility { get; set; } = CommandVisibility.SenderOnly;
}
