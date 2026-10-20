using System.Collections.Concurrent;
using Marvel.Protocol;
using System.Security.Cryptography;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>Owns live capabilities, invitations, and their durable authority records.</summary>
internal sealed class SessionAuthorityRegistry(ISessionCapabilityIssuer issuer)
{
    private readonly ConcurrentDictionary<string, SessionAccess> sessions =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PendingInvitation> invitations =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, byte> retiredClaims = new(StringComparer.Ordinal);

    public bool RetiredEntry(EntryClaim claim) => retiredClaims.ContainsKey(EntryReceipts.Verifier(claim));

    public void RetireClaims(IEnumerable<StoredAuthority> authorities)
    {
        foreach (StoredEntryReceipt receipt in authorities.Select(item => item.Entry).OfType<StoredEntryReceipt>())
            retiredClaims.TryAdd(receipt.Verifier, 0);
    }

    private readonly SessionCapabilityPool capabilities = new(issuer);

    public bool TryEntry(EntryClaim claim, out SessionAccess access)
    {
        string verifier = EntryReceipts.Verifier(claim);
        access = sessions.Values.FirstOrDefault(value => value.Entry?.Verifier == verifier)!;
        return access is not null;
    }

    public object? SessionGate(EngineRequest request)
    {
        if (EntryReceipts.Valid(request.Entry) && TryEntry(request.Entry!, out var claimed)) return claimed.Session.Gate;
        if (TrySession(request, out _, out var access)) return access.Session.Gate;
        return TryInvitation(request.Capability ?? string.Empty, request.GameId, out _, out var invitation)
            ? invitation.Session.Gate : null;
    }

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
            || found.Revoked || !Matches(found.Session, request.GameId))
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

    public string Issue(HashSet<string>? reserved = null) => capabilities.Issue(
        verifier => sessions.ContainsKey(verifier) || invitations.ContainsKey(verifier), reserved);

    public void PublishOpen(
        HostedSession session,
        string capability,
        ViewScope scope,
        IReadOnlyList<(SeatScope Grant, string Token)> issuedInvitations,
        StoredEntryReceipt entry)
    {
        sessions.TryAdd(Verifier(capability), new SessionAccess(session, scope, Owner: true, entry));
        foreach (var (grant, token) in issuedInvitations)
        {
            invitations.TryAdd(Verifier(token), new PendingInvitation(session, grant.Scope));
        }
    }

    public void PublishAttachment(
        string invitationVerifier,
        string capability,
        PendingInvitation pending, StoredEntryReceipt entry)
    {
        invitations.TryRemove(invitationVerifier, out _);
        sessions.TryAdd(
            Verifier(capability),
            new SessionAccess(pending.Session, pending.Scope, Owner: false, entry));
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
        [.. Snapshot(session).Select(authority => authority.Verifier == verifier
            ? authority with { Revoked = true } : authority)];

    public void Revoke(string verifier)
    {
        if (sessions.TryGetValue(verifier, out var access)) sessions[verifier] = access with { Revoked = true };
    }

    public void Remove(HostedSession session)
    {
        RetireClaims(Snapshot(session));
        foreach (string capability in sessions
                     .Where(pair => ReferenceEquals(pair.Value.Session, session))
                     .Select(pair => pair.Key)
                     .ToList())
        {
            sessions.TryRemove(capability, out _);
        }

        foreach (string invitation in invitations
                     .Where(pair => ReferenceEquals(pair.Value.Session, session))
                     .Select(pair => pair.Key)
                     .ToList())
        {
            invitations.TryRemove(invitation, out _);
        }
    }

    internal void RemoveGames(IReadOnlyList<string> gameIds)
    {
        foreach (var session in sessions.Values.Select(item => item.Session).Distinct()
                     .Where(session => gameIds.Contains(session.GameId, StringComparer.Ordinal)).ToArray())
            Remove(session);
    }

    public void ValidateForRestore(StoredSession stored, Game game)
    {
        StoredSessionJson.ValidateLoaded(stored);
        var authorityVerifiers = new HashSet<string>(StringComparer.Ordinal);
        var claimVerifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (StoredAuthority authority in stored.Authorities)
        {
            if (authority.Entry is { } receipt && (!claimVerifiers.Add(receipt.Verifier)
                || retiredClaims.ContainsKey(receipt.Verifier)
                || sessions.Values.Any(access => access.Entry?.Verifier == receipt.Verifier)))
                throw new SessionSaveException("stored entry claim is duplicated");
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
                    invitations.TryAdd(
                        authority.Verifier, new PendingInvitation(session, scope));
                }
                else
                {
                    sessions.TryAdd(
                        authority.Verifier,
                        new SessionAccess(session, scope, authority.Owner, authority.Entry, authority.Revoked));
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
                    Invitation: false, pair.Value.Entry, pair.Value.Revoked))
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
