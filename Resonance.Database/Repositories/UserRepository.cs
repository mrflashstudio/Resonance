using Microsoft.EntityFrameworkCore;
using System.Data;
using Npgsql;
using Resonance.Core.Extensions;
using Resonance.Core.Models;
using Resonance.Core.Repositories;
using Resonance.Database.Entities;
using Resonance.Database.Mapping;

namespace Resonance.Database.Repositories;

public sealed class UserRepository(IDbContextFactory<ResonanceDbContext> database) : IUserRepository
{
    public async Task<Account?> GetAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        return await db.Users.AsNoTracking().Where(user => user.Id == id)
            .Select(UserMapping.AccountProjection).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        return await db.Users.AsNoTracking().Where(user => user.Username == username)
            .Select(UserMapping.AccountProjection).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ChangePrivilegesAsync(int id, UserPrivileges privileges, bool grant, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        return await db.Users.Where(user => user.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(user => user.Privileges, user => grant ? user.Privileges | privileges : user.Privileges & ~privileges),
            cancellationToken) != 0;
    }

    public async Task<bool> ChangeBadgesAsync(int id, UserBadges badges, bool grant, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        return await db.Users.Where(user => user.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(user => user.Badges, user => grant ? user.Badges | badges : user.Badges & ~badges),
            cancellationToken) != 0;
    }

    public async Task<Account?> ChangeSilenceAsync(int id, DateTimeOffset? silencedAt, DateTimeOffset? expiresAt, string? reason,
        CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        var changed = await db.Users.Where(user => user.Id == id && !user.IsBot && !user.IsSuperuser)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.SilencedAt, silencedAt)
                .SetProperty(user => user.SilenceExpiresAt, silencedAt == null ? null : expiresAt)
                .SetProperty(user => user.SilenceReason, silencedAt == null ? null : reason), cancellationToken);

        if (changed == 0)
            return null;

        return await db.Users.AsNoTracking().Where(user => user.Id == id)
            .Select(UserMapping.AccountProjection).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Username == username, cancellationToken);

        return user is null ? null : new UserCredentials(UserMapping.ToAccount(user), user.PasswordHash);
    }

    public async Task<RegistrationResult?> CreateAsync(string username, string passwordHash, DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        if (await db.Users.AnyAsync(user => user.Username == username, cancellationToken))
            return null;

        var user = new User { Username = username, PasswordHash = passwordHash, CreatedAt = createdAt };
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            if (await db.Users.AnyAsync(existing => existing.Username == username, cancellationToken))
                return null;

            throw;
        }

        return new RegistrationResult(user.Id, user.Username);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        return await db.Users.AnyAsync(user => user.Id == id, cancellationToken);
    }

    public async Task RecordActivityAsync(int id, DateTimeOffset observedAt, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        await db.Users.Where(user => user.Id == id && (user.LastOnlineAt == null || user.LastOnlineAt < observedAt))
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastOnlineAt, observedAt), cancellationToken);
    }

    public async Task<Account?> InitializeServerBotAsync(string name, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.OrderBy(user => user.Id).FirstOrDefaultAsync(cancellationToken);
        if (user is not null && !user.IsBot)
            return null;

        if (user is null)
        {
            user = new User { Username = name, CreatedAt = createdAt, IsBot = true };
            db.Users.Add(user);
        }

        user.Username = name;
        user.CountryCode = "AQ";
        user.PasswordHash = string.Empty;
        await db.SaveChangesAsync(cancellationToken);
        return UserMapping.ToAccount(user);
    }

    public async Task<bool> HasSuperuserAsync(CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        return await db.Users.AnyAsync(user => user.IsSuperuser, cancellationToken);
    }

    public async Task<bool> TryClaimSuperuserAsync(int userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (await db.Users.AnyAsync(user => user.IsSuperuser, cancellationToken))
                return false;

            var user = await db.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
            if (user is null || user.IsBot || !UserMapping.ToAccount(user).CanChat(now))
                return false;

            user.IsSuperuser = true;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
        {
            return false;
        }
    }
}
