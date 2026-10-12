namespace Resonance.Core.Models;

[Flags]
public enum UserBadges : long
{
    None = 0,
    LeadDeveloper = 1L << 0,
    GameDeveloper = 1L << 1,
    WebDeveloper = 1L << 2,
    EarlyBird = 1L << 3,
    BugHunter = 1L << 4,
    Tester = 1L << 5,
    ContentCreator = 1L << 6,
    Uwu = 1L << 7,
    Rct = 1L << 8,
    Mmt = 1L << 9,
    Chicken = 1L << 10
}
