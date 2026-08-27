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
    /// Piercing — <c>rr:piercing</c>. An attack that discards tough status
    /// cards from the character it is about to damage.
    /// </summary>
    public const string Piercing = "piercing";

    /// <summary>
    /// Ranged — <c>rr:ranged</c>. An attack that ignores
    /// <c>rr:retaliate-x</c>.
    /// </summary>
    public const string Ranged = "ranged";

    /// <summary>Every keyword this engine grants by name.</summary>
    /// <remarks>
    /// The three above, and no more. A keyword the engine reads off a printed
    /// field — steady, stalwart, retaliate — is not here, because granting one
    /// is adding to that field rather than naming the keyword.
    /// </remarks>
    public static IReadOnlySet<string> Granted { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Overkill, Piercing, Ranged };

    /// <summary>
    /// Whether a card has a keyword, printed on it or granted to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two sources and the rules do not rank them. <c>rr:keywords</c> makes a
    /// keyword "an attribute that conveys specific rules to its card", and a
    /// card can be given one — Charge grants <c>rr:overkill</c> to an attack
    /// for its duration, which is a lasting effect naming the keyword.
    /// </para>
    /// <para>
    /// The printed side is a named attribute; the granted side is a
    /// <see cref="ContinuousEffect"/> whose <c>Kind</c> is the keyword. Neither
    /// of the three here — overkill, piercing, ranged — is printed as an
    /// attribute anywhere in the pool, so today they arrive the second way
    /// only; asking both is what stops that being an assumption.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="card">The card.</param>
    /// <param name="keyword">The keyword.</param>
    /// <param name="facts">The printed card data.</param>
    public static bool Has(
        State.World world, State.Card card, string keyword, State.ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);

        foreach (var effect in world.Effects.Active())
        {
            if (string.Equals(effect.Kind, keyword, StringComparison.Ordinal)
                && effect.Affects == card.ObjectId)
            {
                return true;
            }
        }

        return facts.Attributes(card.FaceId).ContainsKey(Printed(keyword));
    }

    // The printed spelling of a keyword, which the card data capitalises.
    private static string Printed(string keyword) =>
        keyword.Length == 0 ? keyword : char.ToUpperInvariant(keyword[0]) + keyword[1..];

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

        return State.FacedownDrones.Kind(card, facts) != State.CardKind.Minion
            || State.FacedownDrones.BaseValue(card, facts, "Villainous", players) > 0;
    }
}
