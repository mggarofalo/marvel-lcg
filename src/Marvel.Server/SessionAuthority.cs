using System.Security.Cryptography;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>A live game and the durable trace that reconstructs it.</summary>
internal sealed class HostedSession(string gameId, Game game, SessionSave save)
{
    public string GameId { get; } = gameId;

    public Game Game { get; private set; } = game;

    public SessionSave Save { get; private set; } = save;

    public long Revision => Save.Revision;

    public void Publish(Game candidate, SessionSave proposed)
    {
        Game = candidate;
        Save = proposed;
    }

    public void Publish(SessionSave proposed) => Save = proposed;
}

/// <summary>An authenticated capability and its authorized view of one session.</summary>
internal sealed record SessionAccess(HostedSession Session, ViewScope Scope, bool Owner);

/// <summary>A one-time capability that may become authenticated session access.</summary>
internal sealed record PendingInvitation(HostedSession Session, ViewScope Scope);

/// <summary>Owns live capabilities, invitations, and their durable authority records.</summary>
internal sealed class SessionAuthorityRegistry(ISessionCapabilityIssuer issuer)
{
    private readonly Dictionary<string, SessionAccess> sessions =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingInvitation> invitations =
        new(StringComparer.Ordinal);

    public int? AuthorizedSeat(EngineRequest request)
    {
        if (string.IsNullOrEmpty(request.Capability))
        {
            return null;
        }

        string verifier = Verifier(request.Capability);
        return sessions.TryGetValue(verifier, out SessionAccess? access)
            && Matches(access.Session, request.GameId)
            ? access.Scope.SoleSeat
            : invitations.TryGetValue(verifier, out PendingInvitation? invitation)
                && Matches(invitation.Session, request.GameId)
                ? invitation.Scope.SoleSeat
                : null;
    }

    public long? CurrentRevision(EngineRequest request) =>
        string.IsNullOrEmpty(request.Capability)
            ? null
            : sessions.TryGetValue(
                Verifier(request.Capability), out SessionAccess? access)
                && Matches(access.Session, request.GameId)
                ? access.Session.Revision
                : null;

    public bool TrySession(
        EngineRequest request, out string capability, out SessionAccess access)
    {
        capability = request.Capability ?? string.Empty;
        string verifier = Verifier(capability);
        access = null!;
        if (capability.Length is <= 0 or > EngineProtocol.MaximumIdentifierLength
            || !sessions.TryGetValue(verifier, out SessionAccess? found)
            || !Matches(found.Session, request.GameId))
        {
            return false;
        }

        access = found;
        return true;
    }

    public bool TryInvitation(
        string capability, string gameId, out string verifier, out PendingInvitation pending)
    {
        verifier = Verifier(capability);
        pending = null!;
        if (capability.Length is <= 0 or > EngineProtocol.MaximumIdentifierLength
            || !invitations.TryGetValue(verifier, out PendingInvitation? found)
            || !Matches(found.Session, gameId))
        {
            return false;
        }

        pending = found;
        return true;
    }

    public string Issue(HashSet<string>? reserved = null)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            string capability = issuer.Issue();
            string verifier = Verifier(capability);
            if (capability.Length is > 0 and <= EngineProtocol.MaximumIdentifierLength
                && !sessions.ContainsKey(verifier)
                && !invitations.ContainsKey(verifier)
                && !(reserved?.Contains(capability) ?? false))
            {
                return capability;
            }
        }

        throw new InvalidOperationException("could not issue a unique session capability");
    }

    public void PublishOpen(
        HostedSession session,
        string capability,
        ViewScope scope,
        IReadOnlyList<(SeatScope Grant, string Token)> issuedInvitations)
    {
        sessions.Add(Verifier(capability), new SessionAccess(session, scope, Owner: true));
        foreach (var (grant, token) in issuedInvitations)
        {
            invitations.Add(Verifier(token), new PendingInvitation(session, grant.Scope));
        }
    }

    public void PublishAttachment(
        string invitationVerifier,
        string capability,
        PendingInvitation pending)
    {
        invitations.Remove(invitationVerifier);
        sessions.Add(
            Verifier(capability),
            new SessionAccess(pending.Session, pending.Scope, Owner: false));
    }

    public static List<StoredAuthority> OpenAuthorities(
        string capability,
        ViewScope scope,
        int players,
        IReadOnlyList<(SeatScope Grant, string Token)> issuedInvitations)
    {
        var proposed = new List<StoredAuthority>
        {
            Authority(capability, scope, players, owner: true, invitation: false),
        };
        proposed.AddRange(issuedInvitations.Select(pair => Authority(
            pair.Token,
            pair.Grant.Scope,
            players,
            owner: false,
            invitation: true)));
        return proposed;
    }

    public List<StoredAuthority> AttachmentAuthorities(
        HostedSession session,
        string invitationVerifier,
        string capability,
        ViewScope scope) =>
        [
            .. Snapshot(session)
                .Where(authority => authority.Verifier != invitationVerifier)
                .Append(Authority(
                    capability,
                    scope,
                    session.Game.State.Players,
                    owner: false,
                    invitation: false)),
        ];

    public List<StoredAuthority> RevokedAuthorities(
        HostedSession session, string verifier) =>
        [.. Snapshot(session).Where(authority => authority.Verifier != verifier)];

    public void Revoke(string verifier) => sessions.Remove(verifier);

    public void Remove(HostedSession session)
    {
        foreach (string capability in sessions
                     .Where(pair => ReferenceEquals(pair.Value.Session, session))
                     .Select(pair => pair.Key)
                     .ToList())
        {
            sessions.Remove(capability);
        }

        foreach (string invitation in invitations
                     .Where(pair => ReferenceEquals(pair.Value.Session, session))
                     .Select(pair => pair.Key)
                     .ToList())
        {
            invitations.Remove(invitation);
        }
    }

    public void ValidateForRestore(StoredSession stored, Game game)
    {
        StoredSessionJson.ValidateLoaded(stored);
        var authorityVerifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (StoredAuthority authority in stored.Authorities)
        {
            if (authority.Seats.Any(seat => seat >= game.State.Players))
            {
                throw new SessionSaveException(
                    "stored authority seat is outside its game");
            }

            if (!authorityVerifiers.Add(authority.Verifier)
                || sessions.ContainsKey(authority.Verifier)
                || invitations.ContainsKey(authority.Verifier))
            {
                throw new SessionSaveException(
                    "stored authority verifier is duplicated");
            }
        }
    }

    public HostedSession PublishRestored(StoredSession stored, Game game)
    {
        var session = new HostedSession(stored.Save.Session.Label, game, stored.Save);
        try
        {
            foreach (StoredAuthority authority in stored.Authorities)
            {
                var scope = new ViewScope(authority.Seats);
                if (authority.Invitation)
                {
                    invitations.Add(
                        authority.Verifier, new PendingInvitation(session, scope));
                }
                else
                {
                    sessions.Add(
                        authority.Verifier,
                        new SessionAccess(session, scope, authority.Owner));
                }
            }

            return session;
        }
        catch
        {
            Remove(session);
            throw;
        }
    }

    public List<StoredAuthority> Snapshot(HostedSession session) =>
        [
            .. sessions
                .Where(pair => ReferenceEquals(pair.Value.Session, session))
                .Select(pair => new StoredAuthority(
                    pair.Key,
                    ScopeSeats(pair.Value.Scope, session.Game.State.Players),
                    pair.Value.Owner,
                    Invitation: false))
                .Concat(invitations
                    .Where(pair => ReferenceEquals(pair.Value.Session, session))
                    .Select(pair => new StoredAuthority(
                        pair.Key,
                        ScopeSeats(pair.Value.Scope, session.Game.State.Players),
                        Owner: false,
                        Invitation: true)))
                .OrderBy(authority => authority.Verifier, StringComparer.Ordinal),
        ];

    private static StoredAuthority Authority(
        string capability,
        ViewScope scope,
        int players,
        bool owner,
        bool invitation) =>
        new(Verifier(capability), ScopeSeats(scope, players), owner, invitation);

    public static string Verifier(string capability)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(capability);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static IReadOnlyList<int> ScopeSeats(ViewScope scope, int players) =>
        [.. Enumerable.Range(0, players).Where(scope.Includes)];

    private static bool Matches(HostedSession session, string gameId) =>
        string.Equals(session.GameId, gameId, StringComparison.Ordinal);
}

/// <summary>Issues capabilities from operating-system entropy outside gameplay.</summary>
internal sealed class CryptographicCapabilityIssuer : ISessionCapabilityIssuer
{
    public string Issue() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
