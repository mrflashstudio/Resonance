using Resonance.Core.Models;

namespace Resonance.Core.Extensions;

public static class AccountExtensions
{
    public static bool HasAnyPrivilege(this Account account, UserPrivileges privileges)
    {
        return account.IsSuperuser || account.Privileges.HasFlag(UserPrivileges.Developer) ||
            (account.Privileges & privileges) != UserPrivileges.None;
    }

    public static string[] GetBadgeNames(this Account account)
    {
        List<string> badges = [];
        AddPrivilege(UserPrivileges.Developer, "developer");
        AddPrivilege(UserPrivileges.Moderator, "globalmoderator");
        AddPrivilege(UserPrivileges.TeamAssistant, "teamassistant");
        AddPrivilege(UserPrivileges.TeamRanked, "teamranked");
        AddPrivilege(UserPrivileges.Supporter, "supporter");
        AddBadge(UserBadges.LeadDeveloper, "leaddeveloper");
        AddBadge(UserBadges.GameDeveloper, "gamedeveloper");
        AddBadge(UserBadges.WebDeveloper, "webdeveloper");
        AddBadge(UserBadges.EarlyBird, "earlybird");
        AddBadge(UserBadges.BugHunter, "bughunter");
        AddBadge(UserBadges.Tester, "tester");
        AddBadge(UserBadges.ContentCreator, "contentcreator");
        AddBadge(UserBadges.Uwu, "uwu");
        AddBadge(UserBadges.Rct, "rct");
        AddBadge(UserBadges.Mmt, "mmt");
        AddBadge(UserBadges.Chicken, "chicken");
        if (account.IsBot)
            badges.Add("bot");

        return [.. badges];

        void AddPrivilege(UserPrivileges privilege, string name)
        {
            if (account.Privileges.HasFlag(privilege))
                badges.Add(name);
        }

        void AddBadge(UserBadges badge, string name)
        {
            if (account.Badges.HasFlag(badge))
                badges.Add(name);
        }
    }
}
