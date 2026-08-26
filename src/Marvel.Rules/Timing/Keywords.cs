namespace Marvel.Rules.Timing;

/// <summary>
/// Keywords, as <see cref="ContinuousEffect.Kind"/> spells them.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:keywords</c>: "keywords are abilities that have a specific game
/// effect", and <c>rr:overkill.1</c> writes one out as the ability it is
/// equivalent to. So a keyword granted to something is a continuous effect
/// naming it, not a flag: a card that grants one for the duration of an attack
/// registers it with that duration and it expires on its own.
/// </para>
/// <para>
/// Only the ones something in this engine grants. A keyword printed on a card is
/// read from the printed data instead — nothing needs to register what is
/// already on the card.
/// </para>
/// </remarks>
public static class Keywords
{
    /// <summary>
    /// Overkill — <c>rr:overkill</c>. Excess damage from an attack that defeats
    /// an ally goes to that ally's controller, and from one that defeats a
    /// minion to the villain.
    /// </summary>
    public const string Overkill = "overkill";

    /// <summary>
    /// Whether this enemy is given a boost card when it activates —
    /// <c>rr:villainous</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:attack-enemy-activation.step.1</c> and
    /// <c>rr:scheme-enemy-activation.step.1</c> say the same thing: "if a
    /// villain, <b>or a minion with the villainous keyword</b>, is attacking,
    /// give it one facedown boost card from the encounter deck. <i>(If a minion
    /// without the villainous keyword is attacking, skip this step.)</i>"
    /// </para>
    /// <para>
    /// So the boost card is a villain's by default and a minion's only by
    /// keyword — 49 minions in the pool carry it and the rest do not.
    /// <b>Skipping matters beyond the icons</b>: taking a card off the encounter
    /// deck moves every later deal.
    /// </para>
    /// </remarks>
    /// <param name="card">The activating enemy.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="players">How many players are in the game.</param>
    public static bool IsBoosted(State.Card card, State.ICardFacts facts, int players)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);

        return facts.Kind(card.FaceId) != State.CardKind.Minion
            || facts.PrintedValue(card.FaceId, "Villainous", players) > 0;
    }
}
