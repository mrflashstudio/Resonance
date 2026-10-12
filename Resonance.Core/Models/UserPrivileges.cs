namespace Resonance.Core.Models;

[Flags]
public enum UserPrivileges : long
{
    None = 0,
    Developer = 1L << 0,
    Moderator = 1L << 1,
    TeamAssistant = 1L << 2,
    TeamRanked = 1L << 3,
    Supporter = 1L << 4
}
