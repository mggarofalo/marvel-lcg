using System.Text.Json.Serialization;

namespace Marvel.Rules.Prompts;

/// <summary>
/// One decision put to one player: what they may do, and why they are being
/// asked.
/// </summary>
/// <param name="Player">Whose decision this is.</param>
/// <param name="Asking">What is being asked for — see <see cref="Question"/>.</param>
/// <param name="When">
/// The tier this is being asked in — <see cref="Timing.TimingPriority"/>.
/// <c>Untimed</c> for a question that is not timed around an occurrence, which
/// a turn option is not.
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
/// This is the other half of the engine's return value:
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
/// The numbers quoted throughout these types were measured once, over 30 games,
/// 1,997 prompts and 6,351 options. They are the sample that shaped the design;
/// nothing re-measures them.
/// </para>
/// </remarks>
public sealed record Prompt(
    int Player,
    Question Asking,
    Timing.TimingPriority When,
    string Trigger,
    string Label,
    bool Cancellable,
    IReadOnlyList<Affordance> Affordances)
{
    /// <summary>Visible game objects whose occurrence caused this decision.</summary>
    /// <remarks>
    /// This is an engine-authored presentation relation, not a legality input.
    /// The view boundary removes identifiers whose faces are not readable to
    /// the receiving scope.
    /// </remarks>
    public IReadOnlyList<int> ContextCardIds { get; init; } = [];

    /// <summary>Readable engine-authored context for the pending decision.</summary>
    public string? Description { get; init; }

    /// <summary>Engine-authored, display-ready name of the question being asked.</summary>
    /// <remarks>
    /// This structured field is distinct from <see cref="Label"/>, whose
    /// wording remains the domain trace. Consumers render this value directly
    /// and never recover a question by parsing prose.
    /// </remarks>
    public string? DisplayQuestion { get; init; }

    /// <summary>
    /// Whether producing this prompt made concealed candidate identities knowable.
    /// </summary>
    /// <remarks>
    /// Internal ledger metadata, not a client wire field. A target request's
    /// search flag is only a presentation hint and may select already-public
    /// cards, so hidden-candidate producers set this separately.
    /// </remarks>
    [JsonIgnore]
    public bool ExposesConcealedCandidates { get; init; }
}
