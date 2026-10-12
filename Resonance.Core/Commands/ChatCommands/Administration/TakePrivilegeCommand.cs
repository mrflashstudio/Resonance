using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Commands.ChatCommands.Administration;

[ChatCommand("takeprivilege", Usage = "/takeprivilege <user id or username> <value or name>", RequiredPrivileges = UserPrivileges.Developer,
    Description = "Remove one or more privileges from an account by name or numeric value", Example = "/takeprivilege 2 Moderator")]
public sealed class TakePrivilegeCommand(IUserRepository users) : AccountFlagCommand<UserPrivileges>(users, "Removed privileges")
{
    protected override Task<bool> ChangeAsync(int id, UserPrivileges flags, CancellationToken cancellationToken) =>
        Users.ChangePrivilegesAsync(id, flags, false, cancellationToken);
}
