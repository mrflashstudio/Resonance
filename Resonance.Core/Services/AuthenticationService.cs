using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Resonance.Core.Authentication;
using Resonance.Core.Configuration;
using Resonance.Core.Extensions;
using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Services;

public sealed class AuthenticationService(
    IUserRepository users,
    ISessionRepository sessions,
    IPasswordHasher<Account> passwords,
    IOptions<SessionSettings> options,
    TimeProvider clock,
    IPresence presence,
    IValidator<RegistrationInput> registrationValidator) : IAuthenticationService, IDisposable
{
    private static readonly TimeSpan MutationTimeout = TimeSpan.FromSeconds(10);
    private static readonly Account MissingAccount = new(0, string.Empty);
    private readonly string _missingPasswordHash = passwords.HashPassword(MissingAccount, SessionToken.Create());
    private readonly SessionCache _cache = new();
    private readonly SemaphoreSlim _sessionOperations = new(1, 1);
    // Guarded by _sessionOperations. Failed deletes remain denied until a retry succeeds.
    private readonly HashSet<(string Hash, SessionClient Client)> _pendingRevocations = [];
    private readonly HashSet<int> _pendingUserRevocations = [];

    private bool IsFresh(StoredSession? session, DateTimeOffset now)
    {
        return session is not null && session.ExpiresAt > now &&
            now < session.ExpiresAt - options.Value.Lifetime + SessionSettings.RenewalInterval;
    }

    private void Invalidate(Guid id)
    {
        _cache.Remove(id);
        presence.DisconnectSession(id);
    }

    private async Task<StoredSession?> UseAsync(StoredSession session, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        if (session.ExpiresAt <= now)
        {
            Invalidate(session.Id);
            await sessions.DeleteInvalidAsync(session.Id, now, cancellationToken);
            return null;
        }

        if (!IsFresh(session, now))
        {
            var expiresAt = now + options.Value.Lifetime;
            bool renewed;

            try
            {
                renewed = await sessions.RenewAsync(session.Id, now, expiresAt, cancellationToken);
            }
            catch
            {
                // Persistence may have committed. Reload it before making another renewal decision.
                Invalidate(session.Id);
                throw;
            }

            if (!renewed)
            {
                Invalidate(session.Id);
                await sessions.DeleteInvalidAsync(session.Id, now, cancellationToken);
                return null;
            }

            session = session with { ExpiresAt = expiresAt };
        }

        _cache.Store(session);
        return session;
    }

    public async Task<RegistrationOutcome> RegisterAsync(string username, string password, CancellationToken cancellationToken)
    {
        var validation = await registrationValidator.ValidateAsync(new RegistrationInput(username, password), cancellationToken);
        var errors = new Dictionary<string, string[]>(validation.ToDictionary());

        if (errors.Count != 0)
            return new RegistrationOutcome(null, errors);

        var account = new Account(0, username);
        var passwordHash = passwords.HashPassword(account, PasswordProof.Create(password));

        var result = await users.CreateAsync(username, passwordHash, clock.GetUtcNow(), cancellationToken);

        return new RegistrationOutcome(result, errors);
    }

    public async Task<LoginResult> LoginAsync(string login, string passwordProof, SessionClient client,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(client))
            return LoginResult.Failure("Unsupported client.");

        var proof = PasswordProof.Normalize(passwordProof);

        if (proof is null || login is not { Length: >= 3 and <= 32 })
            return LoginResult.Failure("Invalid login or password.");

        var credentials = await users.FindByUsernameAsync(login, cancellationToken);
        var account = credentials?.Account;
        var result = passwords.VerifyHashedPassword(account ?? MissingAccount,
            credentials?.PasswordHash ?? _missingPasswordHash, proof);

        if (account is null || result == PasswordVerificationResult.Failed)
            return LoginResult.Failure("Invalid login or password.");

        if (account.Status.GetEffectiveStatus(account.StatusExpiresAt, clock.GetUtcNow()) == UserStatus.Excluded)
            return LoginResult.Failure("This account is excluded.");

        var passwordHash = result == PasswordVerificationResult.SuccessRehashNeeded
            ? passwords.HashPassword(account, proof)
            : null;
        var token = SessionToken.Create();
        await _sessionOperations.WaitAsync(cancellationToken);

        try
        {
            if (_pendingUserRevocations.Contains(account.Id))
                return LoginResult.Failure("Session revocation is pending. Please try again later.");

            var current = await users.GetAsync(account.Id, cancellationToken);

            if (current is null || current.Status.GetEffectiveStatus(current.StatusExpiresAt, clock.GetUtcNow()) == UserStatus.Excluded)
                return LoginResult.Failure("This account is excluded.");

            var now = clock.GetUtcNow();
            var session = new NewSession(Guid.NewGuid(), account.Id, SessionToken.Hash(token)!, client,
                now, now + options.Value.Lifetime);
            await sessions.CreateAsync(session, passwordHash, cancellationToken);
            _cache.Store(new StoredSession(session.Id, session.UserId, session.TokenHash, session.Client, session.ExpiresAt));
        }
        finally
        {
            _sessionOperations.Release();
        }

        return LoginResult.Success(token, account.Id);
    }

    public Task<AuthenticatedSession?> GetSessionAsync(string token, SessionClient client, CancellationToken cancellationToken)
        => FindSessionAsync(token, client, true, cancellationToken);

    public Task<AuthenticatedSession?> GetSessionForLogoutAsync(string token, SessionClient client, CancellationToken cancellationToken)
        => FindSessionAsync(token, client, false, cancellationToken);

    private async Task<AuthenticatedSession?> FindSessionAsync(string token, SessionClient client, bool renew,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.IsDefined(client))
            return null;

        var hash = SessionToken.Hash(token);

        if (hash is null)
            return null;

        var cached = _cache.Find(hash, client);

        if (IsFresh(cached, clock.GetUtcNow()) ||
            (!renew && cached is not null && cached.ExpiresAt > clock.GetUtcNow()))
            return new AuthenticatedSession(cached!.Id, cached.UserId, cached.ExpiresAt);

        await _sessionOperations.WaitAsync(cancellationToken);

        try
        {
            if (_pendingRevocations.Contains((hash, client)))
                return null;

            var session = _cache.Find(hash, client) ??
                await sessions.FindActiveAsync(hash, client, clock.GetUtcNow(), cancellationToken);

            if (session is null)
            {
                await sessions.DeleteInvalidAsync(hash, client, clock.GetUtcNow(), cancellationToken);
                return null;
            }

            if (_pendingUserRevocations.Contains(session.UserId))
                return null;

            if (renew)
                session = await UseAsync(session, cancellationToken);
            else if (session.ExpiresAt <= clock.GetUtcNow())
                return null;

            return session is null ? null : new AuthenticatedSession(session.Id, session.UserId, session.ExpiresAt);
        }
        finally
        {
            _sessionOperations.Release();
        }
    }

    public async Task<bool> IsSessionActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsFresh(_cache.Find(id), clock.GetUtcNow()))
            return true;

        await _sessionOperations.WaitAsync(cancellationToken);

        try
        {
            var session = _cache.Find(id) ?? await sessions.FindActiveAsync(id, clock.GetUtcNow(), cancellationToken);

            if (session is null)
            {
                await sessions.DeleteInvalidAsync(id, clock.GetUtcNow(), cancellationToken);
                return false;
            }

            if (_pendingRevocations.Contains((session.TokenHash, session.Client)) ||
                _pendingUserRevocations.Contains(session.UserId))
                return false;

            return await UseAsync(session, cancellationToken) is not null;
        }
        finally
        {
            _sessionOperations.Release();
        }
    }

    public DateTimeOffset? GetSessionExpiry(Guid id)
    {
        var session = _cache.Find(id);

        return session is not null && session.ExpiresAt > clock.GetUtcNow() ? session.ExpiresAt : null;
    }

    public async Task RevokeAsync(string token, SessionClient client, CancellationToken cancellationToken)
    {
        var hash = SessionToken.Hash(token);

        if (hash is null)
            return;

        await _sessionOperations.WaitAsync(cancellationToken);

        try
        {
            _pendingRevocations.Add((hash, client));
            var cached = _cache.Find(hash, client);

            if (cached is not null)
                Invalidate(cached.Id);

            using var timeout = new CancellationTokenSource(MutationTimeout, clock);
            var id = await sessions.DeleteAsync(hash, client, timeout.Token);

            if (id is not null && id != cached?.Id)
                Invalidate(id.Value);

            _pendingRevocations.Remove((hash, client));
        }
        finally
        {
            _sessionOperations.Release();
        }
    }

    public async Task RevokeUserSessionsAsync(int userId, CancellationToken cancellationToken)
    {
        await _sessionOperations.WaitAsync(cancellationToken);

        try
        {
            _pendingUserRevocations.Add(userId);
            var cached = _cache.RemoveUser(userId);

            foreach (var id in cached)
                presence.DisconnectSession(id);

            using var timeout = new CancellationTokenSource(MutationTimeout, clock);
            var persisted = await sessions.DeleteUserAsync(userId, timeout.Token);

            foreach (var id in persisted.Except(cached))
                presence.DisconnectSession(id);

            _pendingUserRevocations.Remove(userId);
        }
        finally
        {
            _sessionOperations.Release();
        }
    }

    public async Task RemoveExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        await _sessionOperations.WaitAsync(cancellationToken);

        try
        {
            // Retry failed revocations before pruning. Keep denials if any retry still fails.
            var failures = new List<Exception>();
            foreach (var (hash, client) in _pendingRevocations.ToArray())
            {
                try
                {
                    using var timeout = new CancellationTokenSource(MutationTimeout, clock);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                    var id = await sessions.DeleteAsync(hash, client, linked.Token);
                    if (id is not null)
                        Invalidate(id.Value);
                    _pendingRevocations.Remove((hash, client));
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    failures.Add(exception);
                }
            }

            foreach (var userId in _pendingUserRevocations.ToArray())
            {
                try
                {
                    using var timeout = new CancellationTokenSource(MutationTimeout, clock);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                    foreach (var id in await sessions.DeleteUserAsync(userId, linked.Token))
                        Invalidate(id);
                    _pendingUserRevocations.Remove(userId);
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    failures.Add(exception);
                }
            }

            var now = clock.GetUtcNow();
            await sessions.RemoveExpiredAsync(now, cancellationToken);

            foreach (var id in _cache.RemoveExpired(now))
                presence.DisconnectSession(id);

            if (failures.Count != 0)
                throw new AggregateException("Some session revocations are still pending.", failures);
        }
        finally
        {
            _sessionOperations.Release();
        }
    }

    public void Dispose()
    {
        _sessionOperations.Dispose();
    }
}
