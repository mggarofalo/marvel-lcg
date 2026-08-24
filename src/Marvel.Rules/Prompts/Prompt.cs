namespace Marvel.Rules.Prompts;

/// <summary>Why the engine is asking, which decides how a client frames it.</summary>
/// <remarks>
/// MARVEL-41 requires the prompt to carry "enough context to tell a
/// mid-resolution prompt from a turn-level one". This is that context, and it is
/// an enum the engine already maintains rather than something to infer from a
/// trigger name.
/// </remarks>
public enum PromptKind
{
    /// <summary>A turn-level decision. 90.4% of sampled prompts.</summary>
    Normal,

    /// <summary>A window to respond after something happened. 6.4%.</summary>
    Response,

    /// <summary>A window to interrupt something about to happen. 3.2%.</summary>
    Interrupt,

    /// <summary>An interrupt that must be taken. 0.1%.</summary>
    ForcedInterrupt,
}

/// <summary>
/// One decision put to one player: what they may do, and why they are being
/// asked.
/// </summary>
/// <param name="Player">Whose decision this is.</param>
/// <param name="Kind">
/// Turn-level or mid-resolution — see <see cref="PromptKind"/>.
/// </param>
/// <param name="Trigger">
/// The timing point that opened this, e.g. <c>WhenPlayerInTurn</c>. The same
/// string the event stream carries, so an event and the prompt it came from can
/// be tied together.
/// </param>
/// <param name="Label">
/// The domain-level prompt text, e.g. <c>"Spider-Man resolves mulligans"</c>.
/// </param>
/// <param name="Cancellable">
/// Whether declining is a legal answer. 81% of sampled prompts are cancellable,
/// which matters because 34.8% offer exactly one affordance — without this a
/// client cannot tell "your only move" from "your only move, or pass".
/// </param>
/// <param name="Affordances">What the player may do.</param>
/// <remarks>
/// <para>
/// This is the other half of the fold's return value:
/// </para>
/// <code>
/// (state, input) -> (state, Prompt?, GameEvent[])
/// </code>
/// <para>
/// A prompt is absent when the game is over. It is never empty: a decision with
/// no options is not put to a player. The event list, by contrast, is very often
/// empty — 35.3% of recorded steps change no state at all — so the two are
/// deliberately not symmetrical.
/// </para>
/// <para>
/// The numbers quoted throughout these types were measured by
/// <c>py_src/tools/affordances/census.py</c> over 30 games, 1,997 prompts and
/// 6,351 options. See <c>docs/affordances.md</c>.
/// </para>
/// </remarks>
public sealed record Prompt(
    int Player,
    PromptKind Kind,
    string Trigger,
    string Label,
    bool Cancellable,
    IReadOnlyList<Affordance> Affordances);
