using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>One command sent through either engine transport.</summary>
/// <param name="Version">The protocol version.</param>
/// <param name="RequestId">An opaque client correlation id.</param>
/// <param name="Operation"><c>setup</c>, <c>open</c>, <c>attach</c>, <c>sync</c>, <c>resolve</c>, <c>undo</c>, <c>redo</c>, <c>reorder</c>, or <c>close</c>.</param>
/// <param name="GameId">An opaque id chosen by the client for this game.</param>
/// <param name="Capability">The server-issued session capability; absent only for <c>open</c>.</param>
/// <param name="Game">Present only for <c>open</c>.</param>
/// <param name="Decision">Present only for <c>resolve</c>.</param>
/// <param name="ExpectedRevision">
/// The host revision of the prompt or history position being changed. This is an
/// engine wire choice, not a game rule; it prevents an answer composed for an
/// earlier prompt from being applied to a later prompt with reusable ids.
/// </param>
/// <param name="Viewer">
/// What the opening client says it displays. The server's visibility policy is
/// the authority; this assertion can never widen it.
/// </param>
/// <param name="Cursor">
/// The desired active history-unit count for <c>undo</c> or <c>redo</c>.
/// The cursor is a server ledger position, not a game rule.
/// </param>
/// <param name="Order">
/// Original active unit positions in their desired contiguous order for
/// <c>reorder</c>. The client names no decisions or derived state.
/// </param>
public sealed record EngineRequest(
    int Version,
    string RequestId,
    string Operation,
    string GameId,
    string? Capability = null,
    GameSpecification? Game = null,
    EngineDecision? Decision = null,
    ViewerClaim? Viewer = null,
    long? ExpectedRevision = null,
    int? Cursor = null,
    IReadOnlyList<int>? Order = null)
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
        string requestId,
        string gameId,
        string capability,
        EngineDecision decision,
        long expectedRevision = 0) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Resolve, gameId,
            Capability: capability,
            Decision: decision,
            ExpectedRevision: expectedRevision);

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

    /// <summary>Builds a request to replace the live table with an earlier prefix.</summary>
    public static EngineRequest UndoGame(
        string requestId,
        string gameId,
        string capability,
        int cursor,
        long expectedRevision) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Undo, gameId,
            Capability: capability,
            ExpectedRevision: expectedRevision,
            Cursor: cursor);

    /// <summary>Builds a request to restore retained history after an undo.</summary>
    public static EngineRequest RedoGame(
        string requestId,
        string gameId,
        string capability,
        int cursor,
        long expectedRevision) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Redo, gameId,
            Capability: capability,
            ExpectedRevision: expectedRevision,
            Cursor: cursor);

    /// <summary>Builds a request to rewrite contiguous committed action units.</summary>
    public static EngineRequest ReorderGame(
        string requestId,
        string gameId,
        string capability,
        IReadOnlyList<int> order,
        long expectedRevision) =>
        new(EngineProtocol.Version, requestId, EngineProtocol.Reorder, gameId,
            Capability: capability,
            ExpectedRevision: expectedRevision,
            Order: order);

    /// <summary>Builds a close-game request for the current protocol.</summary>
    public static EngineRequest CloseGame(
        string requestId, string gameId, string capability) =>
        new(
            EngineProtocol.Version, requestId, EngineProtocol.Close, gameId,
            Capability: capability);
}
