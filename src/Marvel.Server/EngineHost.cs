using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
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
    private readonly Dictionary<string, SessionAccess> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingInvitation> invitations = new(StringComparer.Ordinal);

    /// <summary>Creates an engine host with cryptographically random session capabilities.</summary>
    public EngineHost(
        IGameFactory factory,
        ISessionCapabilityIssuer? capabilities = null,
        IVisibilityPolicy? visibility = null)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.capabilities = capabilities ?? new CryptographicCapabilityIssuer();
        this.visibility = visibility ?? new PermissiveVisibilityPolicy();
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

    private EngineResponse Open(EngineRequest request)
    {
        if (request.Game is null || request.Decision is not null)
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
            additionalScopes = visibility.AdditionalScopes(
                request.Viewer, request.Game.Heroes.Count);
        }
        catch (Exception failure) when (failure is ArgumentException or InvalidOperationException)
        {
            return Failed(request, "invalid_request", failure.Message);
        }

        var opened = factory.Create(request.Game);
        var session = new HostedSession(request.GameId, opened.Game);
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
        sessions.Add(capability, new SessionAccess(session, scope, Owner: true));
        foreach (var (grant, token) in issuedInvitations)
        {
            invitations.Add(token, new PendingInvitation(session, grant.Scope));
        }

        return response;
    }

    private EngineResponse Attach(EngineRequest request)
    {
        if (request.Game is not null || request.Decision is not null || request.Viewer is not null)
        {
            return Failed(
                request, "invalid_request",
                "attach accepts only a server-issued invitation");
        }

        string invitation = request.Capability ?? string.Empty;
        if (invitation.Length is <= 0 or > EngineProtocol.MaximumIdentifierLength
            || !invitations.TryGetValue(invitation, out PendingInvitation? pending)
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
            capability);
        invitations.Remove(invitation);
        sessions.Add(
            capability, new SessionAccess(pending.Session, pending.Scope, Owner: false));
        return response;
    }

    private EngineResponse Sync(EngineRequest request)
    {
        if (request.Game is not null || request.Decision is not null || request.Viewer is not null)
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
            access.Scope);
    }

    private EngineResponse Resolve(EngineRequest request)
    {
        if (request.Decision is null || request.Game is not null || request.Viewer is not null)
        {
            return Failed(
                request, "invalid_request",
                "resolve requires decision and does not accept game");
        }

        if (!TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        if (access.Session.Game.Pending is not { } pending
            || !access.Scope.Includes(pending.Player))
        {
            return Failed(request, "not_your_turn", "this capability cannot answer the pending prompt");
        }

        if (request.Decision.Targets is null)
        {
            return Failed(request, "invalid_request", "decision.targets is required");
        }

        try
        {
            var resolved = access.Session.Game.Resolve(request.Decision.ToDomain());
            return Succeeded(
                request,
                access.Session.Game,
                resolved.Prompt,
                resolved.Events,
                access.Scope);
        }
        catch (Exception)
        {
            // The rules promise their named unimplemented boundaries throw
            // before mutation, but an unexpected failure has no such contract.
            // The host chooses to fail closed: a session that might now hold a
            // partial resolve is removed rather than serving a plausible wrong
            // board on the next request.
            Remove(access.Session);
            return Failed(
                request,
                "game_aborted",
                "the game was aborted after an engine failure");
        }
    }

    private EngineResponse Close(EngineRequest request)
    {
        if (request.Game is not null || request.Decision is not null || request.Viewer is not null)
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
            Remove(access.Session);
        }
        else
        {
            sessions.Remove(capability);
        }

        return Succeeded(request);
    }

    private bool TrySession(
        EngineRequest request, out string capability, out SessionAccess session)
    {
        capability = request.Capability ?? string.Empty;
        session = null!;
        if (capability.Length is <= 0 or > EngineProtocol.MaximumIdentifierLength
            || !sessions.TryGetValue(capability, out SessionAccess? found)
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
            if (capability.Length is > 0 and <= EngineProtocol.MaximumIdentifierLength
                && !sessions.ContainsKey(capability)
                && !invitations.ContainsKey(capability)
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

    private static EngineResponse Succeeded(
        EngineRequest request,
        Game? game = null,
        Prompt? prompt = null,
        IReadOnlyList<GameEvent>? events = null,
        ViewScope? scope = null,
        string? capability = null,
        IReadOnlyList<SeatInvitation>? invitations = null) =>
        Projected(
            request, game, prompt, events ?? [], scope, capability, invitations);

    private static EngineResponse Projected(
        EngineRequest request,
        Game? game,
        Prompt? prompt,
        IReadOnlyList<GameEvent> events,
        ViewScope? scope,
        string? capability,
        IReadOnlyList<SeatInvitation>? invitations)
    {
        if (game is null || scope is null)
        {
            return new EngineResponse(
                EngineProtocol.Version, request.RequestId, request.GameId,
                capability, Prompt: null, Events: [], Invitations: invitations);
        }

        VisibleResult visible = WorldProjection.For(game.State, prompt, events, scope);
        return new EngineResponse(
            EngineProtocol.Version, request.RequestId, request.GameId,
            capability,
            visible.Prompt,
            visible.Events,
            visible.World,
            Invitations: invitations);
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

    private sealed record HostedSession(string GameId, Game Game);

    private sealed record SessionAccess(HostedSession Session, ViewScope Scope, bool Owner);

    private sealed record PendingInvitation(HostedSession Session, ViewScope Scope);

    private sealed class CryptographicCapabilityIssuer : ISessionCapabilityIssuer
    {
        public string Issue() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }
}

/// <summary>Loads the repository's canonical datasets and deals games from them.</summary>
public sealed class DatasetGameFactory : IGameFactory
{
    private readonly SetupCatalog setup;
    private readonly CardCatalog cards;
    private readonly AbilityBook abilities;

    private DatasetGameFactory(SetupCatalog setup, CardCatalog cards, AbilityBook abilities)
    {
        this.setup = setup;
        this.cards = cards;
        this.abilities = abilities;
    }

    /// <summary>Loads the three datasets beneath <paramref name="dataRoot"/>.</summary>
    public static DatasetGameFactory Load(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        string root = Path.GetFullPath(dataRoot);
        return new DatasetGameFactory(
            SetupCatalog.Parse(Read(root, "setup", "setup.json")),
            CardCatalog.Parse(Read(root, "cards", "cards.json")),
            AbilityCatalog.Parse(Read(root, "abilities", "abilities.json")));
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

    private static string Read(string root, string dataset, string file) =>
        File.ReadAllText(Path.Combine(root, "datasets", dataset, file));

}
