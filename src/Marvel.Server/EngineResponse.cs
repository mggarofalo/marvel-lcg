using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

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
/// <param name="Revision">
/// The host revision of the returned prompt and world. A resolve must echo it
/// as <see cref="EngineRequest.ExpectedRevision"/>.
/// </param>
/// <param name="History">
/// The replay boundaries this capability may currently request. It contains no
/// decisions, concealed card identities, digests, or information-frontier reasons.
/// </param>
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
    SetupChoices? Setup = null,
    long Revision = 0,
    HistoryDescriptor? History = null);
