namespace Marvel.Rules.Prompts;

/// <summary>
/// One thing a player can do right now, anchored to the object they click.
/// </summary>
/// <param name="Id">
/// What gets handed back to take this. Opaque to the client, and valid only
/// within the session that issued it — see the remarks.
/// </param>
/// <param name="Verb">
/// What kind of thing this is — <c>Play</c>, <c>Attack</c>, <c>Thwart</c>,
/// <c>Change_Form</c>, <c>Ask</c>. A domain word, not a UI word.
/// </param>
/// <param name="AnchorId">
/// The board object the player interacts with to take this. Always present:
/// measured informative on 100% of 6,351 sampled options.
/// </param>
/// <param name="AnchorPlayer">Whose board the anchor sits on.</param>
/// <param name="Label">
/// The domain-level label, unchanged from what the original investigation requires of the prompt
/// so the spec suite is unaffected.
/// </param>
/// <param name="Targets">
/// What still has to be chosen before this can resolve, or <c>null</c> when it
/// takes no targets. Informative on <b>86.5%</b> of sampled options.
/// </param>
/// <param name="Costs">
/// What it costs and the ways to pay, per target. Informative on <b>53.5%</b>.
/// Empty when free.
/// </param>
/// <param name="Illegal">
/// Why this cannot be taken, or <c>null</c> when it can.
/// </param>
/// <remarks>
/// <para>
/// <b><paramref name="Id"/> is a handle, not a name.</b> Ids are allocated per
/// session, so an id written down in one run does not necessarily name the same
/// option in another. the original investigation measured the drift and found it real but
/// narrow — nine of 5,809 recorded inputs, all of one ability kind, every one of
/// the nine <b>exactly 25 too high</b>, across four scenes in four different
/// campaigns. A recorded id therefore has to be re-resolved rather than
/// trusted.
/// </para>
/// <para>
/// In that corpus, <paramref name="AnchorId"/> and <paramref name="Verb"/>
/// resolved every drifted input uniquely. Multiplayer requires
/// <paramref name="AnchorPlayer"/> to distinguish who takes an implied shared
/// action, and multiple actions on one card require <paramref name="Label"/>.
/// A consumer that persists an affordance must therefore persist
/// (AnchorId, AnchorPlayer, Verb, Label) plus its zero-based occurrence among
/// exact matches, and never persist the id. Repeated choice nodes in printed
/// order can otherwise be identical on every public field. The rules define
/// no persistent command identifier; this is the engine's wire-format choice.
/// </para>
/// <para>
/// This replaces a list of option strings. The strings were enough for a web
/// client that repaints a document; a game needs to know which card on the table
/// the option belongs to, which is what <paramref name="AnchorId"/> is for.
/// </para>
/// <para>
/// <b>On the size of this record.</b> The original sketch in
/// <c>presentation-layer.md</c> had five fields and omitted both
/// <paramref name="Targets"/> and <paramref name="Costs"/>. Measured against
/// what the engine already renders, those two are informative on 86.5% and 53.5%
/// of options respectively — they are not edge cases, they are most of what a
/// player is actually deciding. See <c>docs/affordances.md</c>.
/// </para>
/// <para>
/// <b>A wire type.</b> Integers, strings and lists of them, no references into
/// engine state — same constraint as the event stream, and for the same two
/// reasons: these cross a socket in the hosted case, and a live reference would
/// let the view layer read hidden state through a field that was only meant to
/// say what is clickable.
/// </para>
/// </remarks>
public sealed record Affordance(
    int Id,
    string Verb,
    int AnchorId,
    int AnchorPlayer,
    string Label,
    TargetRequest? Targets = null,
    IReadOnlyList<CostOption>? Costs = null,
    string? Illegal = null)
{
    /// <summary>Whether the player can actually take this.</summary>
    /// <remarks>
    /// <para>
    /// The engine offers options it knows cannot be taken, carrying the reason,
    /// so a client can grey one out and say why rather than silently omitting
    /// it. "Pay cost, need 3, but only have 2" is far more useful than a card
    /// that is simply not clickable.
    /// </para>
    /// <para>
    /// Not observed once in 6,351 sampled options, nor in the <b>19,103</b>
    /// the original investigation rendered from a larger sample drawn differently. A player who
    /// only takes what they can afford does not surface many.
    /// </para>
    /// <para>
    /// <b>Treat the zero as a gap in the sample, not in the game.</b> An
    /// unaffordable option is a real thing to render — a card greyed out with
    /// "need 3, but only have 2" beside it is far more useful than a card that
    /// is silently not clickable. Require a case rather than a count from
    /// anyone proposing to delete this.
    /// </para>
    /// </remarks>
    public bool IsLegal => Illegal is null;

    /// <summary>The ways to pay, or an empty list when this is free.</summary>
    public IReadOnlyList<CostOption> CostOptions => Costs ?? [];
}
