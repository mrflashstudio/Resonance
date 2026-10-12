using Resonance.Core.Models;

namespace Resonance.Core.Authentication;

internal sealed class SessionCache
{
    private readonly Lock _sync = new();
    private readonly Dictionary<Guid, StoredSession> _sessions = [];
    private readonly Dictionary<(string Hash, SessionClient Client), Guid> _tokens = [];

    public StoredSession? Find(Guid id)
    {
        lock (_sync)
            return _sessions.GetValueOrDefault(id);
    }

    public StoredSession? Find(string hash, SessionClient client)
    {
        lock (_sync)
            return _tokens.TryGetValue((hash, client), out var id) ? _sessions.GetValueOrDefault(id) : null;
    }

    public void Store(StoredSession session)
    {
        lock (_sync)
        {
            _sessions[session.Id] = session;
            _tokens[(session.TokenHash, session.Client)] = session.Id;
        }
    }

    public void Remove(Guid id)
    {
        lock (_sync)
        {
            if (_sessions.Remove(id, out var session))
                _tokens.Remove((session.TokenHash, session.Client));
        }
    }

    public Guid[] RemoveUser(int userId)
    {
        lock (_sync)
        {
            var ids = _sessions.Values.Where(session => session.UserId == userId).Select(session => session.Id).ToArray();

            foreach (var id in ids)
                Remove(id);

            return ids;
        }
    }

    public Guid[] RemoveExpired(DateTimeOffset now)
    {
        lock (_sync)
        {
            var ids = _sessions.Values.Where(session => session.ExpiresAt <= now).Select(session => session.Id).ToArray();

            foreach (var id in ids)
                Remove(id);

            return ids;
        }
    }
}
