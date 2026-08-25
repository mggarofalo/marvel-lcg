using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Fold;

/// <summary>
/// What one fold produces: the state, the next question, and what happened.
/// </summary>
/// <param name="State">
/// The world after the input was applied. <b>The same instance that went in.</b>
/// </param>
/// <param name="Prompt">The next decision, or <c>null</c> when the game is over.</param>
/// <param name="Events">
/// Everything that changed, in the order it changed. Empty when nothing did.
/// </param>
/// <remarks>
/// <para>
/// This is the signature <c>docs/presentation-layer.md</c> settles on:
/// </para>
/// <code>
/// (state, input) -> (state, Prompt?, GameEvent[])
/// </code>
/// <para>
/// <b><paramref name="State"/> is returned, not copied.</b> The fold mutates the
/// world in place and hands back the same object. That is the decision the
/// presentation-layer document already made — <i>"re-fold from a snapshot plus
/// inputs rather than using persistent data structures"</i> — and it is why
/// undo and replay are built on re-folding rather than on structural sharing. It
/// is on the return value anyway, because the shape is the contract and a caller
/// should not have to know which of the two it is holding.
/// </para>
/// <para>
/// <b>The two halves are not symmetrical.</b> A prompt is absent only when the
/// game is over, and is never empty — a decision with no options is not put to a
/// player. The event list is empty often: 35.3% of recorded steps change no
/// state at all.
/// </para>
/// </remarks>
public sealed record FoldResult(
    World State,
    Prompt? Prompt,
    IReadOnlyList<GameEvent> Events)
{
    /// <summary>Whether the game ended on this fold.</summary>
    public bool IsOver => Prompt is null;
}
