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
    private readonly Dictionary<string, SessionAccess> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingInvitation> invitations = new(StringComparer.Ordinal);

    /// <summary>Creates an engine host with cryptographically random session capabilities.</summary>
    public EngineHost(
        IGameFactory factory,
        ISessionCapabilityIssuer? capabilities = null,
        IVisibilityPolicy? visibility = null,
        ISessionStore? store = null)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.capabilities = capabilities ?? new CryptographicCapabilityIssuer();
        this.visibility = visibility ?? new PermissiveVisibilityPolicy();
        this.store = store ?? new MemorySessionStore();
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

        try
        {
            return Handle(request);
        }
        catch (Exception)
        {
            // A socket cannot throw a domain exception into its caller. The
            // same conversion happens here, before either transport, so local
            // play and hosted play have the same observable failure path. A
            // stack trace is server state and never crosses the boundary.
            return Failed(
                request,
                "engine_error",
                "the engine request failed without changing an existing session");
        }
    }

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
            || request.ExpectedRevision is not null)
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
            || request.ExpectedRevision is not null)
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
                new SeatInvitation(pair.grant.Seat, pair.token)).ToList());
        var proposedAuthorities = new List<StoredAuthority>
        {
            Authority(capability, scope, request.Game.Heroes.Count, owner: true, invitation: false),
        };
        proposedAuthorities.AddRange(issuedInvitations.Select(pair =>
            Authority(pair.token, pair.grant.Scope, request.Game.Heroes.Count,
                owner: false, invitation: true)));
        store.Commit(new StoredSession(save, proposedAuthorities));
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
            || request.ExpectedRevision is not null)
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
            revision: pending.Session.Revision);
        var authorities = Authorities(pending.Session)
            .Where(authority => authority.Verifier != invitationVerifier)
            .Append(Authority(
                capability,
                pending.Scope,
                pending.Session.Game.State.Players,
                owner: false,
                invitation: false))
            .ToList();
        store.Commit(new StoredSession(pending.Session.Save, authorities));
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
            || request.ExpectedRevision is not null)
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
            revision: access.Session.Revision);
    }

    private EngineResponse Resolve(EngineRequest request)
    {
        if (request.Decision is null
            || request.Game is not null
            || request.Viewer is not null
            || request.ExpectedRevision is null or < 0)
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
            Game candidate = SessionReplay.Verify(
                access.Session.Save,
                compatibility,
                ReplayOpen);
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
            string role = UnitRole(candidate, candidatePrompt, replayDecision);
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
            SessionSave proposed = Append(
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
                exposures);
            store.Commit(new StoredSession(proposed, Authorities(access.Session)));
            access.Session.Game = candidate;
            access.Session.Save = proposed;
            return Succeeded(
                request,
                candidate,
                resolved.Prompt,
                resolved.Events,
                access.Scope,
                revision: proposed.Revision);
        }
        catch (Exception failure) when (failure is IOException
            or UnauthorizedAccessException
            or SessionSaveException
            or ReplayDivergenceException)
        {
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

    private EngineResponse Close(EngineRequest request)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null)
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
                Session = access.Session.Save.Session with { Lifecycle = "retired" },
            };
            store.Commit(new StoredSession(retired, []));
            Remove(access.Session);
        }
        else
        {
            string verifier = Verifier(capability);
            var authorities = Authorities(access.Session)
                .Where(authority => authority.Verifier != verifier)
                .ToList();
            store.Commit(new StoredSession(access.Session.Save, authorities));
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
        foreach (StoredSession stored in store.Load())
        {
            if (stored.Save.Session.Lifecycle == "retired")
            {
                continue;
            }

            StoredSession current = stored;
            if (current.Save.Schema == 1)
            {
                SessionSave migrated = SessionReplay.MigrateSchemaOne(
                    current.Save, compatibility, ReplayOpen);
                current = current with { Save = migrated };
                // Publish only after replay has verified the predecessor trace and
                // the complete schema 2 generation is durable.
                store.Commit(current);
            }

            Game game = SessionReplay.Verify(current.Save, compatibility, ReplayOpen);
            var session = new HostedSession(current.Save.Session.Label, game, current.Save);
            foreach (StoredAuthority authority in current.Authorities)
            {
                if (authority.Seats.Any(seat => seat >= game.State.Players))
                {
                    throw new SessionSaveException("stored authority seat is outside its game");
                }

                var scope = new ViewScope(authority.Seats);
                if (authority.Invitation)
                {
                    invitations.Add(authority.Verifier, new PendingInvitation(session, scope));
                }
                else
                {
                    sessions.Add(
                        authority.Verifier,
                        new SessionAccess(session, scope, authority.Owner));
                }
            }
        }
    }

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
        var units = save.Units.Select(unit => unit with
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

    private static string UnitRole(Game game, Prompt prompt, Decision decision)
    {
        if (game.IsForcedResolutionPrompt)
        {
            return "forced_resolution";
        }

        if (game.Phase != GamePhase.PlayerTurn)
        {
            return "phase_step";
        }

        string? verb = decision.IsDecline
            ? null
            : prompt.Affordances.Single(option => option.Id == decision.Affordance).Verb;
        return string.Equals(verb, Game.ChangeForm, StringComparison.Ordinal)
            || string.Equals(verb, Game.EndPhaseVerb, StringComparison.Ordinal)
            || decision.IsDecline
                ? "turn_control"
                : "turn_action";
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
        long revision = 0) =>
        Projected(
            request, game, prompt, events ?? [], scope, capability, invitations, revision);

    private static EngineResponse Projected(
        EngineRequest request,
        Game? game,
        Prompt? prompt,
        IReadOnlyList<GameEvent> events,
        ViewScope? scope,
        string? capability,
        IReadOnlyList<SeatInvitation>? invitations,
        long revision)
    {
        if (game is null || scope is null)
        {
            return new EngineResponse(
                EngineProtocol.Version, request.RequestId, request.GameId,
                capability,
                Prompt: null,
                Events: [],
                Invitations: invitations,
                Revision: revision);
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
            Revision: revision);
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
                typeof(DatasetGameFactory).Assembly.GetName().Version?.ToString() ?? "unknown",
                "engine-replay-v1",
                "mt19937-iso-cxx",
                "state-digest-v2",
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
        ]);

    private static byte[] ReadBytes(string root, string dataset, string file) =>
        File.ReadAllBytes(Path.Combine(root, "datasets", dataset, file));

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

}
