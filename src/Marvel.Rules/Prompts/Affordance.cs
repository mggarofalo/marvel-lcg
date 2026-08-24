namespace Marvel.Rules.Prompts;

/// <summary>
/// One thing a player can do right now, anchored to the object they click.
/// </summary>
/// <param name="Id">
/// What gets folded back in to take this. Opaque to the client.
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
/// The domain-level label, unchanged from what MARVEL-41 requires of the prompt
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
    /// Not observed in the 6,351 options sampled for <c>docs/affordances.md</c>
    /// — a bot that plays what it can afford does not surface many. The
    /// mechanism is real and predates this type: the Python client already greys
    /// out on it. Treat the zero as a gap in the sample, not in the engine.
    /// </para>
    /// </remarks>
    public bool IsLegal => Illegal is null;

    /// <summary>The ways to pay, or an empty list when this is free.</summary>
    public IReadOnlyList<CostOption> CostOptions => Costs ?? [];
}
