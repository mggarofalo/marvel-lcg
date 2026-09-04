using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.View;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Marvel.Server;

/// <summary>The endpoint behind every transport.</summary>
public interface IEngineEndpoint
{
    /// <summary>Applies one protocol request synchronously.</summary>
    EngineResponse Exchange(EngineRequest request);
}

/// <summary>The only interface a client uses, embedded or hosted.</summary>
public interface IEngineTransport
{
    /// <summary>Sends one engine command and receives its result.</summary>
    ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates a fresh game without deciding where its content bytes came from.</summary>
public interface IGameFactory
{
    /// <summary>Deals and opens one game.</summary>
    OpenedGame Create(GameSpecification specification);
}

/// <summary>A game factory whose dataset identities make durable replay meaningful.</summary>
public interface IDurableGameFactory : IGameFactory
{
    /// <summary>The replay contracts and dataset hashes used by this factory.</summary>
    SessionCompatibility Compatibility { get; }
}

/// <summary>Exposes the authored choices a client may use to open a game.</summary>
public interface ISetupDiscovery
{
    /// <summary>Returns the complete supported setup surface.</summary>
    SetupChoices DiscoverSetup();
}

/// <summary>Issues transport capabilities that never enter deterministic game state.</summary>
public interface ISessionCapabilityIssuer
{
    /// <summary>Returns a new opaque capability.</summary>
    string Issue();
}

/// <summary>A game and the card-text events produced during its setup.</summary>
public sealed record OpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);

/// <summary>A single-threaded collection of deterministic engine sessions.</summary>
/// <remarks>
/// The host is deliberately synchronous and has no lock. Socket I/O finishes
/// before a request reaches this type, and the standalone server handles one
/// request at a time. Threading or async must never become a path into game
/// state; see <c>AGENTS.md</c>, “Determinism is load-bearing”.
/// </remarks>
public sealed class EngineHost : IEngineEndpoint
{
    private readonly IGameFactory factory;
    private readonly ISessionCapabilityIssuer capabilities;
    private readonly IVisibilityPolicy visibility;
    private readonly ISessionStore store;
    private readonly SessionCompatibility compatibility;
    private readonly OperationalLog log;
    private readonly Dictionary<string, SessionAccess> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingInvitation> invitations = new(StringComparer.Ordinal);
    private bool replayDiverged;
    private bool sessionRetired;

    /// <summary>Creates an engine host with cryptographically random session capabilities.</summary>
    public EngineHost(
        IGameFactory factory,
        ISessionCapabilityIssuer? capabilities = null,
        IVisibilityPolicy? visibility = null,
        ISessionStore? store = null,
        OperationalLog? log = null)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.capabilities = capabilities ?? new CryptographicCapabilityIssuer();
        this.visibility = visibility ?? new PermissiveVisibilityPolicy();
        this.store = store ?? new MemorySessionStore();
        this.log = log ?? OperationalLog.None;
        if (store is not null && factory is not IDurableGameFactory)
        {
            throw new ArgumentException(
                "persistent hosts require a factory with replay compatibility identities",
                nameof(factory));
        }

        compatibility = factory is IDurableGameFactory durable
            ? durable.Compatibility
            : TestCompatibility();
        Restore();
    }

    /// <inheritdoc />
    public EngineResponse Exchange(EngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var elapsed = Stopwatch.StartNew();
        replayDiverged = false;
        sessionRetired = false;
        int? authorizedSeat = AuthorizedSeat(request);
        long? priorRevision = CurrentRevision(request);
        EngineResponse response;

        try
        {
            response = Handle(request);
        }
        catch (PersistenceFailureException)
        {
            response = Failed(
                request,
                "save_failed",
                "the requested change was not committed and prior durable state remains authoritative");
        }
        catch (Exception)
        {
            // A socket cannot throw a domain exception into its caller. The
            // same conversion happens here, before either transport, so local
            // play and hosted play have the same observable failure path. A
            // stack trace is server state and never crosses the boundary.
            response = Failed(
                request,
                "engine_error",
                "the engine request failed without changing an existing session");
        }

        elapsed.Stop();
        bool accepted = response.Error is null;
        bool mutates = request.Operation is EngineProtocol.Open
            or EngineProtocol.Attach
            or EngineProtocol.Resolve
            or EngineProtocol.Undo
            or EngineProtocol.Redo
            or EngineProtocol.Reorder
            or EngineProtocol.Close;
        bool replays = request.Operation is EngineProtocol.Resolve
            or EngineProtocol.Undo
            or EngineProtocol.Redo
            or EngineProtocol.Reorder;
        string disposition = accepted
            ? "accepted"
            : response.Error!.Code.StartsWith("stale_", StringComparison.Ordinal)
                ? "stale"
                : "rejected";
        long? observedRevision = request.Operation switch
        {
            EngineProtocol.Setup => null,
            EngineProtocol.Close => priorRevision,
            _ when accepted => response.Revision,
            _ => priorRevision,
        };
        log.Write(
            OperationalEventIds.RequestCompleted,
            disposition,
            elapsed.ElapsedMilliseconds,
            request.RequestId,
            request.GameId,
            request.Operation,
            observedRevision,
            authorizedSeat,
            saveCommitted: accepted && mutates,
            replayVerified: accepted && replays,
            replayDiverged: replayDiverged,
            sessionRetired: sessionRetired,
            errorCode: response.Error?.Code,
            expectedRevision: request.ExpectedRevision);
        return response;
    }

    private int? AuthorizedSeat(EngineRequest request)
    {
        if (string.IsNullOrEmpty(request.Capability))
        {
            return null;
        }

        string verifier = Verifier(request.Capability);
        return sessions.TryGetValue(verifier, out SessionAccess? access)
            && string.Equals(
                access.Session.GameId, request.GameId, StringComparison.Ordinal)
            ? access.Scope.SoleSeat
            : invitations.TryGetValue(verifier, out PendingInvitation? invitation)
                && string.Equals(
                    invitation.Session.GameId, request.GameId, StringComparison.Ordinal)
                ? invitation.Scope.SoleSeat
                : null;
    }

    private long? CurrentRevision(EngineRequest request) =>
        string.IsNullOrEmpty(request.Capability)
            ? null
            : sessions.TryGetValue(
                Verifier(request.Capability), out SessionAccess? access)
                && string.Equals(
                    access.Session.GameId, request.GameId, StringComparison.Ordinal)
                ? access.Session.Revision
                : null;

    private EngineResponse Handle(EngineRequest request)
    {
        if (request.Version != EngineProtocol.Version)
        {
            return Failed(
                request, "unsupported_version",
                $"protocol {request.Version} is not supported; expected {EngineProtocol.Version}");
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Failed(request, "invalid_request", "request_id is required");
        }

        if (request.RequestId.Length > EngineProtocol.MaximumIdentifierLength)
        {
            return Failed(
                request, "invalid_request",
                $"request_id exceeds {EngineProtocol.MaximumIdentifierLength} characters");
        }

        if (request.Operation == EngineProtocol.Setup)
        {
            return Setup(request);
        }

        if (string.IsNullOrWhiteSpace(request.GameId))
        {
            return Failed(request, "invalid_request", "game_id is required");
        }

        if (request.GameId.Length > EngineProtocol.MaximumIdentifierLength)
        {
            return Failed(
                request, "invalid_request",
                $"game_id exceeds {EngineProtocol.MaximumIdentifierLength} characters");
        }

        return request.Operation switch
        {
            EngineProtocol.Open => Open(request),
            EngineProtocol.Attach => Attach(request),
            EngineProtocol.Sync => Sync(request),
            EngineProtocol.Resolve => Resolve(request),
            EngineProtocol.Undo => MoveHistory(request, undo: true),
            EngineProtocol.Redo => MoveHistory(request, undo: false),
            EngineProtocol.Reorder => ReorderHistory(request),
            EngineProtocol.Close => Close(request),
            _ => Failed(
                request, "invalid_request",
                $"operation '{request.Operation}' is not supported"),
        };
    }

    private EngineResponse Setup(EngineRequest request)
    {
        if (request.GameId is not ""
            || request.Capability is not null
            || request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "setup accepts only a request id");
        }

        if (factory is not ISetupDiscovery discovery)
        {
            return Failed(
                request, "setup_unavailable",
                "setup choices are not available from this host");
        }

        SetupChoices choices = discovery.DiscoverSetup();
        return new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            GameId: string.Empty,
            Capability: null,
            Prompt: null,
            Events: [],
            Setup: choices);
    }

    private EngineResponse Open(EngineRequest request)
    {
        if (request.Game is null
            || request.Decision is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "open requires game and does not accept decision");
        }

        if (request.Capability is not null)
        {
            return Failed(request, "invalid_request", "open does not accept capability");
        }

        if (string.IsNullOrWhiteSpace(request.Game.Scenario))
        {
            return Failed(request, "invalid_request", "a game requires a scenario");
        }

        if (request.Game.Heroes is not { Count: > 0 }
            || request.Game.Heroes.Any(string.IsNullOrWhiteSpace))
        {
            return Failed(request, "invalid_request", "a game requires at least one hero");
        }

        ViewScope scope;
        IReadOnlyList<SeatScope> additionalScopes;
        try
        {
            scope = visibility.Authorize(request.Viewer, request.Game.Heroes.Count);
            if (scope is null)
            {
                throw new ArgumentException("visibility policy returned no primary scope");
            }

            IReadOnlyList<SeatScope> policyScopes = visibility.AdditionalScopes(
                    request.Viewer, request.Game.Heroes.Count)
                ?? throw new ArgumentException(
                    "visibility policy returned no additional-scope collection");
            additionalScopes = policyScopes.ToList();
            ValidateAdditionalScopes(scope, additionalScopes, request.Game.Heroes.Count);
        }
        catch (Exception failure) when (failure is ArgumentException or InvalidOperationException)
        {
            return Failed(request, "invalid_request", failure.Message);
        }

        var opened = factory.Create(request.Game);
        string storageId = NewStorageId();
        SessionSave save = SessionSave.Open(
            compatibility,
            storageId,
            request.GameId,
            ToSessionSetup(request.Game),
            opened.Game,
            opened.SetupEvents);
        var session = new HostedSession(request.GameId, opened.Game, save);
        string capability = IssueCapability();
        var reserved = new HashSet<string>(StringComparer.Ordinal) { capability };
        var issuedInvitations = additionalScopes
            .Select(grant =>
            {
                string token = IssueCapability(reserved);
                reserved.Add(token);
                return (grant, token);
            })
            .ToList();
        EngineResponse response = Succeeded(
            request,
            opened.Game,
            opened.Game.Pending,
            opened.SetupEvents,
            scope,
            capability,
            issuedInvitations.Select(pair =>
                new SeatInvitation(pair.grant.Seat, pair.token)).ToList(),
            history: History(save, scope));
        var proposedAuthorities = new List<StoredAuthority>
        {
            Authority(capability, scope, request.Game.Heroes.Count, owner: true, invitation: false),
        };
        proposedAuthorities.AddRange(issuedInvitations.Select(pair =>
            Authority(pair.token, pair.grant.Scope, request.Game.Heroes.Count,
                owner: false, invitation: true)));
        ObservePersistence(request, () =>
            store.Commit(new StoredSession(save, proposedAuthorities)));
        sessions.Add(Verifier(capability), new SessionAccess(session, scope, Owner: true));
        foreach (var (grant, token) in issuedInvitations)
        {
            invitations.Add(Verifier(token), new PendingInvitation(session, grant.Scope));
        }

        return response;
    }

    private static void ValidateAdditionalScopes(
        ViewScope primary, IReadOnlyList<SeatScope>? grants, int players)
    {
        if (grants is null)
        {
            throw new ArgumentException("visibility policy returned no additional-scope collection");
        }

        var seats = new HashSet<int>();
        foreach (SeatScope? grant in grants)
        {
            if (grant is null)
            {
                throw new ArgumentException("visibility policy returned an empty seat grant");
            }

            if (grant.Seat < 0 || grant.Seat >= players)
            {
                throw new ArgumentException(
                    $"visibility policy seat {grant.Seat} is outside this game");
            }

            if (!seats.Add(grant.Seat))
            {
                throw new ArgumentException(
                    $"visibility policy returned seat {grant.Seat} more than once");
            }

            if (primary.Includes(grant.Seat))
            {
                throw new ArgumentException(
                    $"visibility policy returned primary seat {grant.Seat} as an additional grant");
            }

            if (grant.Scope is null || !grant.Scope.IsExactly(grant.Seat))
            {
                throw new ArgumentException(
                    $"visibility policy grant for seat {grant.Seat} must authorize exactly that seat");
            }
        }
    }

    private EngineResponse Attach(EngineRequest request)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "attach accepts only a server-issued invitation");
        }

        string invitation = request.Capability ?? string.Empty;
        string invitationVerifier = Verifier(invitation);
        if (invitation.Length is <= 0 or > EngineProtocol.MaximumIdentifierLength
            || !invitations.TryGetValue(invitationVerifier, out PendingInvitation? pending)
            || !string.Equals(
                pending.Session.GameId, request.GameId, StringComparison.Ordinal))
        {
            return Failed(request, "session_not_found", "the seat invitation is not valid");
        }

        string capability = IssueCapability();
        EngineResponse response = Succeeded(
            request,
            pending.Session.Game,
            pending.Session.Game.Pending,
            [],
            pending.Scope,
            capability,
            revision: pending.Session.Revision,
            history: History(pending.Session.Save, pending.Scope));
        var authorities = Authorities(pending.Session)
            .Where(authority => authority.Verifier != invitationVerifier)
            .Append(Authority(
                capability,
                pending.Scope,
                pending.Session.Game.State.Players,
                owner: false,
                invitation: false))
            .ToList();
        SessionSave stamped = Stamp(pending.Session.Save);
        ObservePersistence(request, () =>
            store.Commit(new StoredSession(stamped, authorities)));
        pending.Session.Save = stamped;
        invitations.Remove(invitationVerifier);
        sessions.Add(
            Verifier(capability),
            new SessionAccess(pending.Session, pending.Scope, Owner: false));
        return response;
    }

    private EngineResponse Sync(EngineRequest request)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(request, "invalid_request", "sync accepts only a session capability");
        }

        if (!TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        return Succeeded(
            request,
            access.Session.Game,
            access.Session.Game.Pending,
            [],
            access.Scope,
            revision: access.Session.Revision,
            history: History(access.Session.Save, access.Scope));
    }

    private EngineResponse Resolve(EngineRequest request)
    {
        if (request.Decision is null
            || request.Game is not null
            || request.Viewer is not null
            || request.ExpectedRevision is null or < 0
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "resolve requires decision and does not accept game");
        }

        if (!TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        if (request.Decision.Targets is null)
        {
            return Failed(request, "invalid_request", "decision.targets is required");
        }

        if (request.ExpectedRevision != access.Session.Revision)
        {
            return Failed(
                request,
                "stale_decision",
                "the decision was composed for an earlier table revision");
        }

        if (access.Session.Game.Pending is not { } pending)
        {
            return Failed(request, "not_your_turn", "this capability cannot answer the pending prompt");
        }

        Decision decision = request.Decision.ToDomain();
        Affordance? selected = decision.IsDecline
            ? null
            : pending.Affordances.SingleOrDefault(option => option.Id == decision.Affordance);
        if (!decision.IsDecline && selected is null)
        {
            return Failed(request, "invalid_decision", "the selected affordance is not pending");
        }

        int actor = selected is not null
            && string.Equals(selected.Verb, Game.ActionVerb, StringComparison.Ordinal)
            ? selected.AnchorPlayer
            : pending.Player;
        if (!access.Scope.Includes(actor))
        {
            return Failed(request, "not_your_turn", "this capability cannot submit that decision");
        }

        Prompt? authorized = access.Session.Game.PromptFor(actor);
        if (authorized is null
            || (!decision.IsDecline
                && !authorized.Affordances.Any(option => option.Id == decision.Affordance)))
        {
            return Failed(request, "invalid_decision", "the decision is not available to that seat");
        }

        try
        {
            // This is a validation pass over immutable prompt values. It
            // rejects forged targets, payments, variables, allocations and
            // actor seats before the engine can mutate the world.
            _ = DurableDecision.From(actor, pending, decision).Resolve(pending);
        }
        catch (Exception failure) when (failure is InvalidOperationException
            or ReplayDivergenceException)
        {
            return Failed(request, "invalid_decision", "the decision is not legal for that seat");
        }

        try
        {
            Game candidate = ObserveReplay(request, () => SessionReplay.Verify(
                access.Session.Save, compatibility, ReplayOpen));
            Prompt candidatePrompt = candidate.Pending
                ?? throw new ReplayDivergenceException("candidate has no pending prompt");
            JournalReplay.RequirePrompt(
                PromptRecord.From(pending), candidatePrompt, "live prompt");
            Decision replayDecision = DurableDecision.From(actor, pending, decision)
                .Resolve(candidatePrompt);
            bool root = candidate.IsRootPrompt;
            int active = candidate.Active;
            int round = candidate.Round;
            string phase = candidate.Phase.ToString();
            string role = SessionReplay.UnitRole(candidate, candidatePrompt, replayDecision);
            long rngBefore = candidate.State.Random.Generator.WordsConsumed;
            var resolved = candidate.Resolve(replayDecision);
            IReadOnlyList<InformationExposure> exposures = InformationFrontier.Classify(
                candidate.State.Players,
                rngBefore,
                candidate.State.Random.Generator.WordsConsumed,
                resolved.Information,
                resolved.Events,
                candidate.Pending);
            var step = JournalStep.From(
                actor,
                candidatePrompt,
                replayDecision,
                resolved.Events,
                candidate.State.Random.Generator.WordsConsumed,
                SessionReplay.Fingerprint(candidate),
                SessionReplay.Result(candidate));
            SessionSave proposed = Stamp(Append(
                access.Session.Save,
                step,
                root,
                candidate.IsRootPrompt || candidate.Pending is null,
                role,
                actor,
                active,
                round,
                phase,
                candidate.Pending,
                exposures));
            ObservePersistence(request, () =>
                store.Commit(new StoredSession(proposed, Authorities(access.Session))));
            access.Session.Game = candidate;
            access.Session.Save = proposed;
            return Succeeded(
                request,
                candidate,
                resolved.Prompt,
                resolved.Events,
                access.Scope,
                revision: proposed.Revision,
                history: History(proposed, access.Scope));
        }
        catch (Exception failure) when (failure is IOException
            or UnauthorizedAccessException
            or SessionSaveException
            or ReplayDivergenceException)
        {
            replayDiverged = failure is ReplayDivergenceException;
            return Failed(
                request,
                "save_failed",
                "the decision was not committed and the prior game remains authoritative");
        }
        catch (Exception)
        {
            // Resolution runs only on a freshly replayed candidate. Even an
            // unexpected engine failure cannot partially mutate the live game.
            return Failed(
                request,
                "game_aborted",
                "the candidate decision failed and the prior game remains authoritative");
        }
    }

    private EngineResponse MoveHistory(EngineRequest request, bool undo)
    {
        string operation = undo ? EngineProtocol.Undo : EngineProtocol.Redo;
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is null or < 0
            || request.Cursor is null or < 0
            || request.Order is not null)
        {
            return Failed(
                request,
                "invalid_request",
                $"{operation} requires an expected revision and history cursor");
        }

        if (!TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        SessionSave save = access.Session.Save;
        if (request.ExpectedRevision != save.Revision)
        {
            return Failed(
                request,
                "stale_history",
                "the history command was composed for an earlier table revision");
        }

        int target = request.Cursor.Value;
        if (target > save.Units.Count
            || (undo && target >= save.Cursor)
            || (!undo && target <= save.Cursor))
        {
            return Failed(
                request,
                "history_direction",
                $"{operation} cursor is not an available retained boundary");
        }

        if (save.Units.Any(unit => unit.Status != "complete"))
        {
            return Failed(
                request,
                "history_open",
                "history cannot change while an operation has dependent decisions pending");
        }

        int first = Math.Min(target, save.Cursor);
        int count = Math.Abs(target - save.Cursor);
        IReadOnlyList<JournalUnit> affected = save.Units.Skip(first).Take(count).ToList();
        if (!EditableBy(affected, access.Scope))
        {
            return Failed(
                request,
                "history_authority",
                "this capability cannot revise history submitted by another seat");
        }

        if (target < save.EditFrontier)
        {
            return Failed(
                request,
                "history_frontier",
                "new information makes that earlier history boundary unavailable");
        }

        try
        {
            Game candidate = ObserveReplay(request, () =>
                SessionReplay.VerifyAtCursor(save, compatibility, ReplayOpen, target));
            SessionSave proposed = save with
            {
                Compatibility = compatibility,
                Revision = save.Revision + 1,
                Cursor = target,
                CurrentPrompt = candidate.Pending is null
                    ? null
                    : PromptRecord.From(candidate.Pending),
            };
            Game verified = ObserveReplay(request, () =>
                SessionReplay.Verify(proposed, compatibility, ReplayOpen));
            ObservePersistence(request, () =>
                store.Commit(new StoredSession(proposed, Authorities(access.Session))));
            access.Session.Game = verified;
            access.Session.Save = proposed;
            return Succeeded(
                request,
                verified,
                verified.Pending,
                [],
                access.Scope,
                revision: proposed.Revision,
                history: History(proposed, access.Scope));
        }
        catch (Exception failure) when (failure is IOException
            or UnauthorizedAccessException
            or SessionSaveException
            or ReplayDivergenceException)
        {
            replayDiverged = failure is ReplayDivergenceException;
            return Failed(
                request,
                "history_failed",
                "history replay was not committed and the prior game remains authoritative");
        }
    }

    private EngineResponse ReorderHistory(EngineRequest request)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is null or < 0
            || request.Cursor is not null
            || request.Order is not { Count: >= 2 })
        {
            return Failed(
                request,
                "invalid_request",
                "reorder requires an expected revision and at least two unit positions");
        }

        if (!TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        SessionSave save = access.Session.Save;
        if (request.ExpectedRevision != save.Revision)
        {
            return Failed(
                request,
                "stale_history",
                "the history command was composed for an earlier table revision");
        }

        int[] order = [.. request.Order];
        if (order.Any(index => index < 0 || index >= save.Cursor)
            || order.Distinct().Count() != order.Length)
        {
            return Failed(
                request,
                "reorder_shape",
                "reorder positions must name distinct active history units");
        }

        int first = order.Min();
        int last = order.Max();
        if (last - first + 1 != order.Length
            || order.SequenceEqual(Enumerable.Range(first, order.Length)))
        {
            return Failed(
                request,
                "reorder_shape",
                "reorder must change one contiguous range of history units");
        }

        if (save.Units.Any(unit => unit.Status != "complete"))
        {
            return Failed(
                request,
                "history_open",
                "history cannot change while an operation has dependent decisions pending");
        }

        List<JournalUnit> affected = save.Units.Skip(first).Take(order.Length).ToList();
        if (!EditableBy(affected, access.Scope))
        {
            return Failed(
                request,
                "history_authority",
                "this capability cannot revise history submitted by another seat");
        }

        if (first < save.EditFrontier)
        {
            return Failed(
                request,
                "history_frontier",
                "new information makes that history range unavailable");
        }

        JournalUnit position = affected[0];
        if (affected.Any(unit => unit.Role != "turn_action"
                || unit.ActiveSeat != position.ActiveSeat
                || unit.Round != position.Round
                || !string.Equals(unit.Phase, position.Phase, StringComparison.Ordinal)))
        {
            return Failed(
                request,
                "reorder_kind",
                "only action units from one active-player turn can be reordered");
        }

        try
        {
            int[] sourceOrder =
            [
                .. Enumerable.Range(0, first),
                .. order,
                .. Enumerable.Range(last + 1, save.Cursor - last - 1),
            ];
            RewrittenTrace trace = SessionReplay.Rewrite(
                save, compatibility, ReplayOpen, sourceOrder);
            SessionSave proposed = save with
            {
                Compatibility = compatibility,
                Revision = save.Revision + 1,
                Cursor = trace.Units.Count,
                EditFrontier = trace.EditFrontier,
                CurrentPrompt = trace.Game.Pending is null
                    ? null
                    : PromptRecord.From(trace.Game.Pending),
                Units = trace.Units,
            };
            Game verified = ObserveReplay(request, () =>
                SessionReplay.Verify(proposed, compatibility, ReplayOpen));
            ObservePersistence(request, () =>
                store.Commit(new StoredSession(proposed, Authorities(access.Session))));
            access.Session.Game = verified;
            access.Session.Save = proposed;
            return Succeeded(
                request,
                verified,
                verified.Pending,
                [],
                access.Scope,
                revision: proposed.Revision,
                history: History(proposed, access.Scope));
        }
        catch (Exception failure) when (failure is IOException
            or UnauthorizedAccessException
            or SessionSaveException
            or ReplayDivergenceException)
        {
            return Failed(
                request,
                "reorder_failed",
                "the rewritten trace was not committed and the prior game remains authoritative");
        }
    }

    private EngineResponse Close(EngineRequest request)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "close does not accept game or decision");
        }

        if (!TrySession(request, out string capability, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        if (access.Owner)
        {
            SessionSave retired = access.Session.Save with
            {
                Compatibility = compatibility,
                Session = access.Session.Save.Session with { Lifecycle = "retired" },
            };
            ObservePersistence(request, () =>
                store.Commit(new StoredSession(retired, [])));
            Remove(access.Session);
            sessionRetired = true;
        }
        else
        {
            string verifier = Verifier(capability);
            var authorities = Authorities(access.Session)
                .Where(authority => authority.Verifier != verifier)
                .ToList();
            SessionSave stamped = Stamp(access.Session.Save);
            ObservePersistence(request, () =>
                store.Commit(new StoredSession(stamped, authorities)));
            access.Session.Save = stamped;
            sessions.Remove(verifier);
        }

        return Succeeded(request);
    }

    private bool TrySession(
        EngineRequest request, out string capability, out SessionAccess session)
    {
        capability = request.Capability ?? string.Empty;
        string verifier = Verifier(capability);
        session = null!;
        if (capability.Length is <= 0 or > EngineProtocol.MaximumIdentifierLength
            || !sessions.TryGetValue(verifier, out SessionAccess? found)
            || !string.Equals(found.Session.GameId, request.GameId, StringComparison.Ordinal))
        {
            return false;
        }

        session = found;
        return true;
    }

    private T ObserveReplay<T>(EngineRequest request, Func<T> work)
    {
        var elapsed = Stopwatch.StartNew();
        try
        {
            T result = work();
            elapsed.Stop();
            log.Write(
                OperationalEventIds.ReplayCompleted, "accepted",
                elapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "replay", replayVerified: true);
            return result;
        }
        catch (Exception failure)
        {
            elapsed.Stop();
            bool diverged = failure is ReplayDivergenceException;
            log.Write(
                OperationalEventIds.ReplayCompleted, "rejected",
                elapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "replay",
                replayDiverged: diverged,
                errorCode: diverged ? "replay_diverged" : "replay_failed");
            throw;
        }
    }

    private void ObservePersistence(
        EngineRequest request,
        Func<string?> work)
    {
        var elapsed = Stopwatch.StartNew();
        try
        {
            string? saveGeneration = work();
            elapsed.Stop();
            log.Write(
                OperationalEventIds.PersistenceCompleted, "accepted",
                elapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "persistence", saveCommitted: true,
                expectedRevision: request.ExpectedRevision,
                saveGeneration: saveGeneration);
        }
        catch (Exception failure)
        {
            elapsed.Stop();
            log.Write(
                OperationalEventIds.PersistenceCompleted, "rejected",
                elapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "persistence", errorCode: "persistence_failed",
                expectedRevision: request.ExpectedRevision);
            throw new PersistenceFailureException(failure);
        }
    }

    private sealed class PersistenceFailureException(Exception failure)
        : IOException("session persistence failed", failure);

    private string IssueCapability(HashSet<string>? reserved = null)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            string capability = capabilities.Issue();
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

    private void Remove(HostedSession session)
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

    private void Restore()
    {
        foreach (SessionLoadResult candidate in store.LoadForRestore())
        {
            if (candidate.Session is not StoredSession stored)
            {
                log.Write(
                    OperationalEventIds.SessionRestoreFailed,
                    "rejected",
                    gameId: candidate.StorageId,
                    saveGeneration: candidate.Generation,
                    stage: "quarantine",
                    errorCode: candidate.ErrorCode ?? "restore_failed");
                continue;
            }

            if (stored.Save.Session.Lifecycle == "retired")
            {
                continue;
            }

            var elapsed = Stopwatch.StartNew();
            bool saveCommitted = false;
            string? selectedGeneration = candidate.Generation;
            HostedSession? restoring = null;
            try
            {
                StoredSession current = stored;
                if (current.Save.Schema == 1)
                {
                    SessionSave migrated = SessionReplay.MigrateSchemaOne(
                        current.Save, compatibility, ReplayOpen);
                    current = current with { Save = migrated };
                    // Publish only after replay has verified the predecessor trace and
                    // the complete schema 2 generation is durable.
                    selectedGeneration = store.Commit(current);
                    saveCommitted = true;
                }

                Game game = SessionReplay.Verify(current.Save, compatibility, ReplayOpen);
                var session = new HostedSession(current.Save.Session.Label, game, current.Save);
                restoring = session;
                foreach (StoredAuthority authority in current.Authorities)
                {
                    if (authority.Seats.Any(seat => seat >= game.State.Players))
                    {
                        throw new SessionSaveException(
                            "stored authority seat is outside its game");
                    }

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

                elapsed.Stop();
                log.Write(
                    OperationalEventIds.SessionRestored,
                    "accepted",
                    elapsed.ElapsedMilliseconds,
                    gameId: current.Save.Session.Label,
                    revision: current.Save.Revision,
                    saveCommitted: saveCommitted,
                    replayVerified: true,
                    saveGeneration: selectedGeneration,
                    stage: saveCommitted ? "migration" : "restore");
            }
            catch (Exception failure)
            {
                if (restoring is not null)
                {
                    Remove(restoring);
                }

                elapsed.Stop();
                log.Write(
                    OperationalEventIds.SessionRestoreFailed,
                    "rejected",
                    elapsed.ElapsedMilliseconds,
                    gameId: stored.Save.Session.Label,
                    revision: stored.Save.Revision,
                    saveCommitted: saveCommitted,
                    replayDiverged: failure is ReplayDivergenceException,
                    saveGeneration: selectedGeneration,
                    stage: "quarantine",
                    errorCode: failure switch
                    {
                        ReplayDivergenceException => "replay_diverged",
                        SessionCompatibilityException mismatch => mismatch.Category,
                        _ => "restore_failed",
                    });
            }
        }
    }

    private SessionSave Stamp(SessionSave save) => save with
    {
        Compatibility = compatibility,
    };

    private ReplayOpenedGame ReplayOpen(SessionSetup setup)
    {
        OpenedGame opened = factory.Create(new GameSpecification(
            setup.Scenario, setup.Heroes, setup.ModularSets, setup.Seed));
        return new ReplayOpenedGame(opened.Game, opened.SetupEvents);
    }

    private List<StoredAuthority> Authorities(HostedSession session)
    {
        var authorities = sessions
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
            .OrderBy(authority => authority.Verifier, StringComparer.Ordinal)
            .ToList();
        return authorities;
    }

    private static StoredAuthority Authority(
        string capability,
        ViewScope scope,
        int players,
        bool owner,
        bool invitation) =>
        new(Verifier(capability), ScopeSeats(scope, players), owner, invitation);

    private static IReadOnlyList<int> ScopeSeats(ViewScope scope, int players) =>
        [.. Enumerable.Range(0, players).Where(scope.Includes)];

    private static string Verifier(string capability)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(capability);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string NewStorageId() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private static SessionSetup ToSessionSetup(GameSpecification setup) =>
        new(setup.Scenario, [.. setup.Heroes],
            setup.ModularSets is null ? null : [.. setup.ModularSets], setup.Seed);

    private static SessionSave Append(
        SessionSave save,
        JournalStep step,
        bool startsUnit,
        bool completesUnit,
        string role,
        int actor,
        int active,
        int round,
        string phase,
        Prompt? currentPrompt,
        IReadOnlyList<InformationExposure> exposures)
    {
        var units = save.Units.Take(save.Cursor).Select(unit => unit with
        {
            Decisions = [.. unit.Decisions],
        }).ToList();
        if (startsUnit)
        {
            units.Add(new JournalUnit(
                role,
                completesUnit ? "complete" : "open",
                actor,
                active,
                round,
                phase,
                [step],
                exposures));
        }
        else
        {
            if (units.Count == 0 || units[^1].Status != "open")
            {
                throw new ReplayDivergenceException(
                    "a dependent decision has no open history unit");
            }

            JournalUnit open = units[^1];
            units[^1] = open with
            {
                Status = completesUnit ? "complete" : "open",
                Decisions = [.. open.Decisions, step],
                Exposures = InformationFrontier.Merge(open.Exposures, exposures),
            };
        }

        if (currentPrompt is null)
        {
            units[^1] = units[^1] with { Role = "terminal", Status = "complete" };
        }

        return save with
        {
            Revision = save.Revision + 1,
            Cursor = units.Count,
            EditFrontier = exposures.Count > 0 ? units.Count : save.EditFrontier,
            CurrentPrompt = currentPrompt is null ? null : PromptRecord.From(currentPrompt),
            Units = units,
        };
    }

    private HistoryDescriptor History(SessionSave save, ViewScope scope)
    {
        IReadOnlyList<HistoryEntryDescriptor> entries = SessionReplay
            .InspectActiveHistory(save, compatibility, ReplayOpen)
            .Select(unit => new HistoryEntryDescriptor(
                unit.Cursor,
                scope.Includes(unit.Actor) || unit.Outcome is not null
                    ? ActionHistoryPresenter.Present(new ActionHistoryFacts(
                        unit.Cursor,
                        unit.ActorName,
                        unit.Role,
                        unit.Phase,
                        unit.Verb,
                        unit.Action,
                        scope.Includes(unit.Actor) ? unit.ResourceGenerators : [],
                        Enum.TryParse(unit.Outcome, out Outcome outcome)
                            ? outcome
                            : null))
                    : $"{unit.ActorName} completed an action."))
            .ToArray();
        if (save.Units.Any(unit => unit.Status != "complete"))
        {
            return new HistoryDescriptor(save.Cursor, [], [], entries);
        }

        int[] undo = Enumerable.Range(save.EditFrontier, save.Cursor - save.EditFrontier)
            .Where(target => EditableBy(
                save.Units.Skip(target).Take(save.Cursor - target), scope))
            .ToArray();
        int[] redo = Enumerable.Range(save.Cursor + 1, save.Units.Count - save.Cursor)
            .Where(target => EditableBy(
                save.Units.Skip(save.Cursor).Take(target - save.Cursor), scope))
            .ToArray();
        return new HistoryDescriptor(save.Cursor, undo, redo, entries);
    }

    private static bool EditableBy(IEnumerable<JournalUnit> units, ViewScope scope)
    {
        int[] actors = units
            .SelectMany(unit => unit.Decisions
                .Select(step => step.Decision.Actor)
                .Prepend(unit.InitiatingSeat))
            .Distinct()
            .ToArray();
        return actors.Length == 1 && scope.Includes(actors[0]);
    }

    private static SessionCompatibility TestCompatibility() => new(
        Application: "test",
        ReplayContract: "engine-replay-v1",
        RngContract: "mt19937-iso-cxx",
        StateDigest: "state-digest-v2",
        CardsSha256: new string('0', 64),
        SetupSha256: new string('0', 64),
        AbilitiesSha256: new string('0', 64));

    private static EngineResponse Succeeded(
        EngineRequest request,
        Game? game = null,
        Prompt? prompt = null,
        IReadOnlyList<GameEvent>? events = null,
        ViewScope? scope = null,
        string? capability = null,
        IReadOnlyList<SeatInvitation>? invitations = null,
        long revision = 0,
        HistoryDescriptor? history = null) =>
        Projected(
            request, game, prompt, events ?? [], scope, capability, invitations, revision,
            history);

    private static EngineResponse Projected(
        EngineRequest request,
        Game? game,
        Prompt? prompt,
        IReadOnlyList<GameEvent> events,
        ViewScope? scope,
        string? capability,
        IReadOnlyList<SeatInvitation>? invitations,
        long revision,
        HistoryDescriptor? history)
    {
        if (game is null || scope is null)
        {
            return new EngineResponse(
                EngineProtocol.Version, request.RequestId, request.GameId,
                capability,
                Prompt: null,
                Events: [],
                Invitations: invitations,
                Revision: revision,
                History: history);
        }

        Prompt? scopedPrompt = scope.SoleSeat is int seat
            ? game.PromptFor(seat)
            : prompt;
        VisibleResult visible = WorldProjection.For(game.State, scopedPrompt, events, scope);
        return new EngineResponse(
            EngineProtocol.Version, request.RequestId, request.GameId,
            capability,
            visible.Prompt,
            visible.Events,
            visible.World,
            Invitations: invitations,
            Revision: revision,
            History: history);
    }

    private static EngineResponse Failed(
        EngineRequest request, string code, string message) =>
        new(
            EngineProtocol.Version,
            Bounded(request.RequestId, EngineProtocol.MaximumIdentifierLength),
            Bounded(request.GameId, EngineProtocol.MaximumIdentifierLength),
            Capability: null,
            Prompt: null,
            Events: [],
            World: null,
            Error: new EngineError(
                Bounded(code, EngineProtocol.MaximumIdentifierLength),
                Bounded(message, EngineProtocol.MaximumErrorLength)));

    private static string Bounded(string? value, int maximum) => value switch
    {
        null => string.Empty,
        { Length: var length } when length <= maximum => value,
        _ => value[..maximum],
    };

    private sealed class HostedSession(string gameId, Game game, SessionSave save)
    {
        public string GameId { get; } = gameId;

        public Game Game { get; set; } = game;

        public SessionSave Save { get; set; } = save;

        public long Revision => Save.Revision;
    }

    private sealed record SessionAccess(HostedSession Session, ViewScope Scope, bool Owner);

    private sealed record PendingInvitation(HostedSession Session, ViewScope Scope);

    private sealed class CryptographicCapabilityIssuer : ISessionCapabilityIssuer
    {
        public string Issue() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }
}

/// <summary>Loads the repository's canonical datasets and deals games from them.</summary>
public sealed class DatasetGameFactory : IDurableGameFactory, ISetupDiscovery
{
    private readonly SetupCatalog setup;
    private readonly CardCatalog cards;
    private readonly AbilityBook abilities;

    private DatasetGameFactory(
        SetupCatalog setup,
        CardCatalog cards,
        AbilityBook abilities,
        SessionCompatibility compatibility)
    {
        this.setup = setup;
        this.cards = cards;
        this.abilities = abilities;
        Compatibility = compatibility;
    }

    /// <inheritdoc />
    public SessionCompatibility Compatibility { get; }

    /// <summary>Loads the three datasets beneath <paramref name="dataRoot"/>.</summary>
    public static DatasetGameFactory Load(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        string root = Path.GetFullPath(dataRoot);
        byte[] setupBytes = ReadBytes(root, "setup", "setup.json");
        byte[] cardBytes = ReadBytes(root, "cards", "cards.json");
        byte[] abilityBytes = ReadBytes(root, "abilities", "abilities.json");
        return new DatasetGameFactory(
            SetupCatalog.Parse(System.Text.Encoding.UTF8.GetString(setupBytes)),
            CardCatalog.Parse(System.Text.Encoding.UTF8.GetString(cardBytes)),
            AbilityCatalog.Parse(System.Text.Encoding.UTF8.GetString(abilityBytes)),
            new SessionCompatibility(
                EngineBuildIdentity.ProductVersion,
                EngineBuildIdentity.ReplayContract,
                EngineBuildIdentity.RngContract,
                EngineBuildIdentity.StateDigest,
                Hash(cardBytes),
                Hash(setupBytes),
                Hash(abilityBytes)));
    }

    /// <inheritdoc />
    public OpenedGame Create(GameSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        var runner = new AbilityRunner(abilities);
        var setupEvents = new List<GameEvent>();
        var campaign = setup.Campaign(specification.Scenario);
        var world = WorldSetup.Deal(
            cards,
            Blueprints.From(
                Dealer.DealOrder(
                    setup,
                    specification.Scenario,
                    specification.Heroes,
                    specification.ModularSets,
                    cards),
                cards),
            [.. specification.Heroes.Select(hero => setup.Hero(hero).Name)],
            specification.Seed,
            runner,
            setupEvents,
            campaign.Expert);
        return new OpenedGame(Game.Begin(world, cards, runner), setupEvents);
    }

    /// <inheritdoc />
    public SetupChoices DiscoverSetup() => new(
        Heroes:
        [
            .. setup.HeroNames.Select(key =>
                new HeroSetupChoice(key, setup.Hero(key).Name)),
        ],
        Scenarios:
        [
            .. setup.CampaignNames.Select(key =>
            {
                CampaignSetup campaign = setup.Campaign(key);
                return new ScenarioSetupChoice(
                    key, campaign.Name, campaign.Expert, [.. campaign.ModularSets]);
            }),
        ],
        ModularSets:
        [
            .. setup.EncounterSetNames
                .Where(key => ModularEncounterSets.IsModular(setup, cards, key))
                .Select(key => new ModularSetupChoice(
                    key, setup.EncounterSetDisplayName(key))),
        ],
        Runtime: new RuntimeIdentity(
            EngineBuildIdentity.ProductVersion,
            EngineBuildIdentity.Commit,
            Compatibility.ReplayContract,
            Compatibility.RngContract,
            Compatibility.StateDigest,
            EngineProtocol.Version,
            SessionSave.CurrentSchema,
            Compatibility.CardsSha256,
            Compatibility.SetupSha256,
            Compatibility.AbilitiesSha256));

    private static byte[] ReadBytes(string root, string dataset, string file) =>
        File.ReadAllBytes(Path.Combine(root, "datasets", dataset, file));

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

}
