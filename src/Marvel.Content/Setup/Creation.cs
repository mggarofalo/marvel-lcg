namespace Marvel.Content.Setup;

/// <summary>Why a card exists — which setup step asked for it.</summary>
public enum CreationSource
{
    /// <summary>The <c>rule_a,rule_b</c> pseudo-card every world has.</summary>
    Rules,

    /// <summary>A campaign-level challenge card.</summary>
    Challenge,

    /// <summary>The hero, b-face first.</summary>
    Identity,

    /// <summary>Set aside into the player's nemesis pile.</summary>
    Obligation,

    /// <summary>The rest of the nemesis set.</summary>
    Nemesis,

    /// <summary>The identity's signature cards.</summary>
    HeroDeck,

    /// <summary>The aspect cards.</summary>
    PlayerDeck,

    /// <summary>One per entry in the campaign's scheme list.</summary>
    MainScheme,

    /// <summary>Every villain stage, in printed order.</summary>
    Villain,

    /// <summary>The scenario's own encounter cards.</summary>
    Encounter,

    /// <summary>A named encounter set.</summary>
    EncounterSet,
}

/// <summary>
/// One card, at the moment the engine allocates its id.
/// </summary>
/// <param name="Spec">
/// Comma-separated face ids exactly as the card data spells them —
/// <c>01001b,01001a</c> is <b>one card with two faces</b>, not two cards.
/// </param>
/// <param name="Source">Which setup step asked for it.</param>
/// <param name="Player">
/// The seat it was created for, or <see cref="Scenario"/>.
/// </param>
/// <remarks>
/// The position of a <see cref="Creation"/> in the dealt sequence <b>is</b> the
/// card's <c>object_id</c>, and <c>object_id</c> is on the wire in every state
/// digest — checklist item 1 of <c>docs/state-digest-v2.md</c>, "everything else
/// depends on this". So this order is a wire format, not an implementation
/// detail.
/// </remarks>
public sealed record Creation(string Spec, CreationSource Source, int Player)
{
    /// <summary>The seat value for a card that belongs to the scenario.</summary>
    public const int Scenario = -1;

    /// <summary>
    /// The faces, in order. The first one starts face up, with one published
    /// exception: a main scheme is created <c>1A,1B</c> and flipped to its
    /// <c>1B</c> side when it enters play.
    /// </summary>
    public IReadOnlyList<string> Faces => Spec.Split(',');
}
