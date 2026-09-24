using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Resonance.Authentication;
using Resonance.Data;
using Resonance.Data.Entities;
using Resonance.Extensions;
using Resonance.Models;

namespace Resonance.Services;

public sealed class AuthenticationService(
    IDbContextFactory<ResonanceDbContext> database,
    IPasswordHasher<User> passwords,
    IOptions<SessionSettings> options,
    TimeProvider clock,
    IOnlinePlayers onlinePlayers) : IAuthenticationService
{
    private static readonly User MissingUser = new() { Username = string.Empty };
    private readonly string _missingPasswordHash = passwords.HashPassword(MissingUser, SessionToken.Create());

    public async Task<RegistrationResponse?> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);

        if (await db.Users.AnyAsync(user => user.Username == request.Username, cancellationToken))
            return null;

        var user = new User
        {
            Username = request.Username,
            CreatedAt = clock.GetUtcNow()
        };
        user.PasswordHash = passwords.HashPassword(user, PasswordProof.Create(request.Password));
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            if (await db.Users.AnyAsync(existing => existing.Username == request.Username, cancellationToken))
                return null;

            throw;
        }

        return new RegistrationResponse(user.Id, user.Username);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (request.Client is not ("game" or "web"))
            return new LoginResponse("Unsupported client.");

        var proof = PasswordProof.Normalize(request.PasswordProof);

        if (proof is null || request.Login is not { Length: >= 3 and <= 32 })
            return new LoginResponse("Invalid login or password.");

        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(user => user.Username == request.Login, cancellationToken);
        var result = passwords.VerifyHashedPassword(user ?? MissingUser, user?.PasswordHash ?? _missingPasswordHash, proof);

        if (user is null || result == PasswordVerificationResult.Failed)
            return new LoginResponse("Invalid login or password.");

        if (user.Status.GetEffectiveStatus(user.StatusExpiresAt, clock.GetUtcNow()) == UserStatus.Excluded)
            return new LoginResponse("This account is excluded.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwords.HashPassword(user, proof);

        var token = SessionToken.Create();
        var now = clock.GetUtcNow();
        db.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = SessionToken.Hash(token)!,
            Client = request.Client,
            CreatedAt = now,
            ExpiresAt = now + options.Value.Lifetime
        });

        await db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(Session: token, ProfileId: user.Id);
    }

    public async Task<AuthenticatedSession?> GetSessionAsync(string token, string client, CancellationToken cancellationToken)
    {
        var hash = SessionToken.Hash(token);

        if (hash is null)
            return null;

        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var now = clock.GetUtcNow();

        var authenticated = await db.Sessions.AsNoTracking()
            .Where(session => session.TokenHash == hash && session.Client == client &&
                session.RevokedAt == null && session.ExpiresAt > now &&
                (session.User.Status != UserStatus.Excluded || session.User.StatusExpiresAt <= now))
            .Select(session => new AuthenticatedSession(session.Id, new Account(session.User.Id, session.User.Username)
            {
                CreatedAt = session.User.CreatedAt,
                LastOnlineAt = session.User.LastOnlineAt,
                AboutMe = session.User.AboutMe,
                CountryCode = session.User.CountryCode,
                Rank = session.User.Rank,
                RhythmPoints = session.User.RhythmPoints,
                PlayCount = session.User.PlayCount,
                SquaresHit = session.User.SquaresHit,
                TotalScore = session.User.TotalScore,
                Status = session.User.Status,
                StatusExpiresAt = session.User.StatusExpiresAt
            }, session.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);

        return authenticated is null ? null : authenticated with
        {
            Account = authenticated.Account with { IsOnline = onlinePlayers.IsOnline(authenticated.Account.Id) }
        };
    }

    public async Task<bool> IsSessionActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var now = clock.GetUtcNow();

        return await db.Sessions.AnyAsync(session => session.Id == id && session.RevokedAt == null &&
            session.ExpiresAt > now &&
            (session.User.Status != UserStatus.Excluded || session.User.StatusExpiresAt <= now), cancellationToken);
    }

    public async Task RevokeAsync(string token, string client, CancellationToken cancellationToken)
    {
        var hash = SessionToken.Hash(token);

        if (hash is null)
            return;

        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var query = db.Sessions.Where(session => session.TokenHash == hash && session.Client == client);
        var id = await query.Select(session => (Guid?)session.Id).SingleOrDefaultAsync(cancellationToken);

        if (id is null)
            return;

        var now = clock.GetUtcNow();
        await query.Where(session => session.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, now), cancellationToken);

        onlinePlayers.DisconnectSession(id.Value);
    }
}
