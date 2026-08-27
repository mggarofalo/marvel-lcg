using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Turns a deal order into something the rules can lay out.</summary>
/// <remarks>
/// The content layer knows <i>why</i> a card exists; the rules layer only needs
/// to know <i>where it goes</i>. Several sources collapse into one slot — a
/// hero's signature cards and their aspect cards are two different
/// deck-building questions and the same answer about placement.
/// </remarks>
public static class Blueprints
{
    /// <summary>The deal order, as blueprints.</summary>
    /// <remarks>
    /// <para>
    /// <b>The order is untouched and only the destination changes.</b> A
    /// creation's position in this list <i>is</i> the card's object id, which
    /// is on the wire in every digest, so nothing here may reorder anything —
    /// and nothing does. A set's cards land contiguously: in one measured game
    /// the modular set's ran 40147-40150 into the encounter deck at ids
    /// 202-205 and 40151-40158 into the aside pile at ids 206-215, unbroken.
    /// </para>
    /// <para>
    /// <b>The aside pile's own order is not settled.</b> Measurement showed it
    /// varying between seeds of one board, which means it is shuffled — and a
    /// shuffle draws from the game's single random stream, so reproducing one
    /// needs the exact position in that stream rather than merely the right
    /// permutation. This deals in creation order, which is deterministic and
    /// which nothing so far contradicts. See MARVEL-210.
    /// </para>
    /// </remarks>
    /// <param name="dealt">The creations, in allocation order.</param>
    /// <param name="facts">The printed card data, for the keywords that reroute.</param>
    public static IReadOnlyList<CardBlueprint> From(
        IReadOnlyList<Creation> dealt, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(dealt);
        ArgumentNullException.ThrowIfNull(facts);
        return
        [
            .. dealt.Select(creation => new CardBlueprint(
                creation.Spec,
                SetAside(creation, facts) ? SetupSlot.SetAside : SlotFor(creation.Source),
                creation.Player)),
        ];
    }

    /// <summary>
    /// Whether a card begins the game outside every deck —
    /// <c>rr:permanent.2</c>, <c>rr:setup-keyword.1</c>, <c>rr:linked-card-title.1</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three keywords and one destination. <c>rr:permanent.2</c>: "permanent
    /// cards are set aside before step 1 of setup and are put into play later
    /// by abilities on other cards". <c>rr:setup-keyword.1</c>: a setup card is
    /// "put into play during the <i>Put Setup Cards Into Play</i> step of
    /// setup" — so it too starts outside the decks, and is fetched a step
    /// later. <c>rr:linked-card-title.1</c>: "set this card aside during
    /// setup".
    /// </para>
    /// <para>
    /// <b>An identity is never set aside</b> whatever it prints, because it is
    /// the card the player is. Nothing in the pool carries the combination, and
    /// the guard is here so that a future card cannot make a player start with
    /// no hero.
    /// </para>
    /// </remarks>
    private static bool SetAside(Creation creation, ICardFacts facts)
    {
        if (creation.Source == CreationSource.Identity)
        {
            return false;
        }

        // The first face is the one dealt; a card is set aside or not as a
        // whole, and no card in the pool prints one of these on one face only.
        string faceId = creation.Spec.Split(',')[0];
        var printed = facts.Attributes(faceId);
        return printed.ContainsKey("Permanent")
            || printed.ContainsKey("Setup")
            || printed.ContainsKey("Linked");
    }

    private static SetupSlot SlotFor(CreationSource source) => source switch
    {
        CreationSource.Rules => SetupSlot.Rules,
        CreationSource.Challenge => SetupSlot.Challenge,
        CreationSource.Identity => SetupSlot.Identity,
        CreationSource.Obligation => SetupSlot.Obligation,
        CreationSource.Nemesis => SetupSlot.Nemesis,
        CreationSource.HeroDeck or CreationSource.PlayerDeck => SetupSlot.PlayerDeck,
        CreationSource.MainScheme => SetupSlot.MainScheme,
        CreationSource.Villain => SetupSlot.Villain,
        CreationSource.Encounter or CreationSource.EncounterSet => SetupSlot.Encounter,
        CreationSource.ScenarioSetAside => SetupSlot.SetAside,
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };
}
