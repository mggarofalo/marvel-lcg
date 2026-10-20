namespace Marvel.Rules.Prompts;

/// <summary>One engine-offered action with its anchor, target constraints and payment options.</summary>
/// <param name="Id">Opaque handle valid only for the issuing prompt.</param>
/// <param name="Verb">The engine-authored domain action.</param>
/// <param name="AnchorId">Object identity within the namespace named by AnchorKind.</param>
/// <param name="AnchorPlayer">Seat whose board holds the anchor.</param>
/// <param name="Label">Domain label used when identifying the offered choice.</param>
/// <param name="Targets">Required selections, or null when the offer takes no targets.</param>
/// <param name="Costs">Complete alternative costs; null or empty when free.</param>
/// <param name="Illegal">Why the offer cannot be taken, or null when legal.</param>
/// <param name="Description">Readable action text supplied by the engine.</param>
/// <remarks>
/// These are value contracts without references to live game state. View snapshots
/// copy their nested collection storage and filter disclosure for the authorized seat.
/// Clients assess drafts through engine-owned legality; an offered handle alone is
/// neither enduring authority nor a persistent command identity. Replay identifies
/// the current offer through its semantic address, including the anchor namespace,
/// acting seat and occurrence among otherwise identical options. This address is an
/// engine wire-format choice; see docs/affordances.md and docs/session-ledger.md.
/// </remarks>
public sealed record Affordance(
    int Id,
    string Verb,
    int AnchorId,
    int AnchorPlayer,
    string Label,
    TargetRequest? Targets = null,
    IReadOnlyList<CostOption>? Costs = null,
    string? Illegal = null,
    string? Description = null)
{
    /// <summary>Timing of this current engine-offered ability.</summary>
    public Marvel.Rules.Timing.AbilityType? Timing { get; init; }

    /// <summary>Complete current intentions; an empty list retains explicit composition.</summary>
    public IReadOnlyList<WindowChoice> WindowChoices { get; init; } = [];

    /// <summary>Engine-authored display name, separate from the stable command label.</summary>
    public string? DisplayLabel { get; init; }

    /// <summary>Engine-authored action and affected object for accepting this exact option.</summary>
    public string? CommitLabel { get; init; }

    /// <summary>Mandatory non-resource costs, described by the owning engine ability.</summary>
    public string? CostDescription { get; init; }

    /// <summary>The namespace that authoritatively identifies <see cref="AnchorId"/>.</summary>
    /// <remarks>
    /// Existing engine producers anchor to cards. Area producers must opt in;
    /// an unspecified anchor is intentionally rendered as an opaque fallback.
    /// The game rules do not define this UI identity, so this is an engine wire
    /// choice rather than a rules-derived distinction.
    /// </remarks>
    public AffordanceAnchorKind AnchorKind { get; init; } = AffordanceAnchorKind.Card;

    /// <summary>Whether taking this offer initiates playing its source card.</summary>
    /// <remarks>The engine chooses this presentation marker so clients need not infer event plays from names or card text.</remarks>
    public bool PlaysCard { get; init; } = Verb == Play.CardPlay.Verb;

    /// <summary>Whether the admitted effect starts with a separate target choice after this action's costs commit.</summary>
    /// <remarks>
    /// This engine-owned marker supplies no future legal targets. Cancellation or
    /// intervening changes can prevent that choice. False makes no claim about
    /// choices inside conditional or later effect branches.
    /// </remarks>
    public bool DeferredTargetSelection { get; init; }

    /// <summary>A single currently offered recipient completes this support's explicit activation.</summary>
    /// <remarks>Target details disclose the complete mandatory cost and effect. Empty selection
    /// retains the separately prompted route; clients never pre-answer that future prompt.</remarks>
    public bool DirectRecipientCommit { get; init; }

    /// <summary>The source whose current card choice selects this offer's card anchor.</summary>
    /// <remarks>This engine-owned interaction link describes only the current prompt;
    /// it neither offers nor reserves a target for a future decision.</remarks>
    public int? SelectionSourceId { get; init; }

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
