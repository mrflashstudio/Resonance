using Microsoft.EntityFrameworkCore;
using Resonance.Core.Models;
using Resonance.Core.Repositories;
using Resonance.Database.Entities;

namespace Resonance.Database.Repositories;

public sealed class SessionRepository(IDbContextFactory<ResonanceDbContext> database) : ISessionRepository
{
    private static string ClientName(SessionClient client)
    {
        return client switch
        {
            SessionClient.Game => "game",
            SessionClient.Web => "web",
            _ => throw new ArgumentOutOfRangeException(nameof(client))
        };
    }

    private static IQueryable<Session> ActiveSessions(ResonanceDbContext db, DateTimeOffset now)
    {
        return db.Sessions.AsNoTracking().Where(session => session.RevokedAt == null && session.ExpiresAt > now &&
            (session.User.Status != UserStatus.Excluded || session.User.StatusExpiresAt <= now));
    }

    private static IQueryable<Session> InvalidSessions(ResonanceDbContext db, DateTimeOffset now)
    {
        return db.Sessions.Where(session => session.RevokedAt != null || session.ExpiresAt <= now ||
            (session.User.Status == UserStatus.Excluded &&
                (session.User.StatusExpiresAt == null || session.User.StatusExpiresAt > now)));
    }

    private static async Task<StoredSession?> ReadAsync(IQueryable<Session> query, CancellationToken cancellationToken)
    {
        var session = await query.SingleOrDefaultAsync(cancellationToken);

        if (session is null)
            return null;

        var client = session.Client switch
        {
            "game" => SessionClient.Game,
            "web" => SessionClient.Web,
            _ => throw new InvalidOperationException("Unsupported persisted session client.")
        };

        return new StoredSession(session.Id, session.UserId, session.TokenHash, client, session.ExpiresAt);
    }

    public async Task CreateAsync(NewSession session, string? passwordHash, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        if (passwordHash is not null)
        {
            var user = await db.Users.SingleAsync(user => user.Id == session.UserId, cancellationToken);
            user.PasswordHash = passwordHash;
        }

        db.Sessions.Add(new Session
        {
            Id = session.Id,
            UserId = session.UserId,
            TokenHash = session.TokenHash,
            Client = ClientName(session.Client),
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredSession?> FindActiveAsync(string tokenHash, SessionClient client, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var clientName = ClientName(client);
        return await ReadAsync(ActiveSessions(db, now).Where(session => session.TokenHash == tokenHash &&
            session.Client == clientName), cancellationToken);
    }

    public async Task<StoredSession?> FindActiveAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        return await ReadAsync(ActiveSessions(db, now).Where(session => session.Id == id), cancellationToken);
    }

    public async Task<bool> RenewAsync(Guid id, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        return await ActiveSessions(db, now).Where(session => session.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.ExpiresAt, expiresAt), cancellationToken) == 1;
    }

    public async Task<Guid?> DeleteAsync(string tokenHash, SessionClient client, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var clientName = ClientName(client);
        var query = db.Sessions.Where(session => session.TokenHash == tokenHash && session.Client == clientName);
        var id = await query.Select(session => (Guid?)session.Id).SingleOrDefaultAsync(cancellationToken);

        if (id is not null)
            await query.ExecuteDeleteAsync(cancellationToken);

        return id;
    }

    public async Task<Guid[]> DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var query = db.Sessions.Where(session => session.UserId == userId);
        var ids = await query.Select(session => session.Id).ToArrayAsync(cancellationToken);
        await query.ExecuteDeleteAsync(cancellationToken);

        return ids;
    }

    public async Task DeleteInvalidAsync(string tokenHash, SessionClient client, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var clientName = ClientName(client);

        await InvalidSessions(db, now).Where(session => session.TokenHash == tokenHash && session.Client == clientName)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteInvalidAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        await InvalidSessions(db, now).Where(session => session.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RemoveExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        await db.Sessions.Where(session => session.ExpiresAt <= now || session.RevokedAt != null)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
