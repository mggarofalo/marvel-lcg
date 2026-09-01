using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>The versioned request/response protocol shared by both transports.</summary>
public static class EngineProtocol
{
    /// <summary>
    /// The only protocol version this host accepts. It includes independently
    /// scoped seat capabilities, play-area topology events, setup discovery,
    /// and per-target allocation capacities.
    /// </summary>
    public const int Version = 5;

    /// <summary>The largest request or game id accepted or echoed.</summary>
    public const int MaximumIdentifierLength = 256;

    /// <summary>The largest diagnostic text returned to a client.</summary>
    public const int MaximumErrorLength = 1024;

    /// <summary>Starts a game from the named, vendored content.</summary>
    public const string Open = "open";

    /// <summary>Reads the authored choices from which a game can be opened.</summary>
    public const string Setup = "setup";

    /// <summary>Redeems a server-issued one-time invitation to a seat.</summary>
    public const string Attach = "attach";

    /// <summary>Reads the current prompt and snapshot without changing the game.</summary>
    public const string Sync = "sync";

    /// <summary>Applies one decision to an open game.</summary>
    public const string Resolve = "resolve";

    /// <summary>Releases an open game.</summary>
    public const string Close = "close";
}

/// <summary>Everything needed to deal one deterministic game.</summary>
/// <param name="Scenario">The scenario key in <c>datasets/setup/setup.json</c>.</param>
/// <param name="Heroes">Hero keys, in seat order.</param>
/// <param name="ModularSets">
/// Chosen modular-set keys, or null for the scenario's recommended sets. An
/// empty list deliberately means no modular set.
/// </param>
/// <param name="Seed">The seed for the game's one MT19937 stream.</param>
public sealed record GameSpecification(
    string Scenario,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets,
    uint Seed);

/// <summary>One authored hero choice.</summary>
public sealed record HeroSetupChoice(string Key, string Name);

/// <summary>One authored scenario and mode choice.</summary>
public sealed record ScenarioSetupChoice(
    string Key,
    string Name,
    bool Expert,
    IReadOnlyList<string> RecommendedModularSets);

/// <summary>One authored encounter set selectable as a modular set.</summary>
public sealed record ModularSetupChoice(string Key, string Name);

/// <summary>The complete product-selection surface exposed by this host.</summary>
public sealed record SetupChoices(
    IReadOnlyList<HeroSetupChoice> Heroes,
    IReadOnlyList<ScenarioSetupChoice> Scenarios,
    IReadOnlyList<ModularSetupChoice> ModularSets);

/// <summary>The five fields the engine accepts as a decision, and no derived properties.</summary>
/// <remarks>
/// This DTO is separate from <see cref="Decision"/> because that domain type
/// also exposes convenience getters such as <c>Spent</c>. Those getters are not
/// additional wire fields. The spelling here is an engine choice, not a rule.
/// </remarks>
public sealed record EngineDecision(
    int Affordance,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int>? Resources = null,
    IReadOnlyDictionary<string, long>? Values = null,
    IReadOnlyList<ResourceAllocation>? Allocations = null)
{
    internal Decision ToDomain() =>
        new(Affordance, Targets, Resources, Values, Allocations);

    /// <summary>The wire form of a decline.</summary>
    public static EngineDecision Decline { get; } = new(-1, []);
}

/// <summary>One command sent through either engine transport.</summary>
/// <param name="Version">The protocol version.</param>
/// <param name="RequestId">An opaque client correlation id.</param>
/// <param name="Operation"><c>setup</c>, <c>open</c>, <c>attach</c>, <c>sync</c>, <c>resolve</c>, or <c>close</c>.</param>
/// <param name="GameId">An opaque id chosen by the client for this game.</param>
/// <param name="Capability">The server-issued session capability; absent only for <c>open</c>.</param>
/// <param name="Game">Present only for <c>open</c>.</param>
/// <param name="Decision">Present only for <c>resolve</c>.</param>
/// <param name="Viewer">
/// What the opening client says it displays. The server's visibility policy is
/// the authority; this assertion can never widen it.
/// </param>
public sealed record EngineRequest(
    int Version,
    string RequestId,
    string Operation,
    string GameId,
    string? Capability = null,
    GameSpecification? Game = null,
    EngineDecision? Decision = null,
    ViewerClaim? Viewer = null)
{
    /// <summary>Builds a read-only setup-discovery request.</summary>
    public static EngineRequest ReadSetup(string requestId) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Setup, GameId: string.Empty);

    /// <summary>Builds an open-game request for the current protocol.</summary>
    public static EngineRequest OpenGame(
        string requestId,
        string gameId,
        GameSpecification game,
        ViewerClaim? viewer = null) =>
        new(
            EngineProtocol.Version, requestId, EngineProtocol.Open, gameId,
            Game: game, Viewer: viewer);

    /// <summary>Builds a resolve request for the current protocol.</summary>
    public static EngineRequest ResolveGame(
        string requestId, string gameId, string capability, EngineDecision decision) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Resolve, gameId,
            Capability: capability, Decision: decision);

    /// <summary>Builds a request that redeems a one-time seat invitation.</summary>
    public static EngineRequest AttachGame(
        string requestId, string gameId, string invitation) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Attach, gameId,
            Capability: invitation);

    /// <summary>Builds a read-only request for the current authorized view.</summary>
    public static EngineRequest SyncGame(
        string requestId, string gameId, string capability) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Sync, gameId,
            Capability: capability);

    /// <summary>Builds a close-game request for the current protocol.</summary>
    public static EngineRequest CloseGame(
        string requestId, string gameId, string capability) =>
        new(
            EngineProtocol.Version, requestId, EngineProtocol.Close, gameId,
            Capability: capability);
}

/// <summary>A rejected request, represented identically by both transports.</summary>
public sealed record EngineError(string Code, string Message);

/// <summary>A one-time bearer invitation to one server-authorized seat.</summary>
public sealed record SeatInvitation(int Seat, string Invitation);

/// <summary>What the engine host returns after opening, resolving, or closing a game.</summary>
/// <param name="Version">The protocol version.</param>
/// <param name="RequestId">The caller's correlation id.</param>
/// <param name="GameId">The caller's game id.</param>
/// <param name="Capability">The new session capability on <c>open</c> or <c>attach</c>; otherwise null.</param>
/// <param name="Prompt">The next question, or null when the game is over or failed.</param>
/// <param name="Events">Setup or resolution events, in engine order.</param>
/// <param name="World">The client-safe table snapshot, or null on close/failure.</param>
/// <param name="Error">Why the request failed, or null on success.</param>
/// <param name="Invitations">One-time seat invitations returned only to the game opener.</param>
/// <param name="Setup">Authored setup choices returned only by <c>setup</c>.</param>
public sealed record EngineResponse(
    int Version,
    string RequestId,
    string GameId,
    string? Capability,
    Prompt? Prompt,
    IReadOnlyList<GameEvent> Events,
    WorldDescriptor? World = null,
    EngineError? Error = null,
    IReadOnlyList<SeatInvitation>? Invitations = null,
    SetupChoices? Setup = null);
