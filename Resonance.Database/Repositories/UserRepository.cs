using Microsoft.EntityFrameworkCore;
using Npgsql;
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
}
