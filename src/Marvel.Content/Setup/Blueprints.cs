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
    /// Existing creations keep their relative order and destination. Linked
    /// product cards are new creations inserted immediately before the first
    /// bringing card in each deck — <c>rr:linked-card-title.3</c> — and that
    /// deterministic insertion position is part of object-id allocation.
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
        ValidateDeckMaximums(dealt, facts);
        var blueprints = new List<CardBlueprint>();
        var linkedByDeck = new HashSet<(string Deck, string Card)>();
        foreach (var creation in dealt)
        {
            if (DeckOf(creation) is { } deck)
            {
                foreach (string linked in creation.Faces
                             .SelectMany(facts.LinkedCards)
                             .Distinct(StringComparer.Ordinal))
                {
                    if (linkedByDeck.Add((deck, linked)))
                    {
                        // The linked creation precedes the first card that
                        // names it. This allocation spelling is measured engine
                        // behavior; the rulebook decides only that it is set
                        // aside and excluded from deck size.
                        blueprints.Add(new CardBlueprint(
                            linked, SetupSlot.SetAside, creation.Player));
                    }
                }
            }

            blueprints.Add(new CardBlueprint(
                creation.Spec,
                SetAside(creation, facts) ? SetupSlot.SetAside : SlotFor(creation.Source),
                creation.Player));
        }
        return blueprints;
    }

    /// <summary>Refuses a player deck that exceeds a printed per-deck maximum.</summary>
    private static void ValidateDeckMaximums(
        IReadOnlyList<Creation> dealt, ICardFacts facts)
    {
        var deckCards = dealt.Where(creation =>
            creation.Player >= 0
            && creation.Source is CreationSource.HeroDeck or CreationSource.PlayerDeck);
        foreach (var deck in deckCards.GroupBy(creation => creation.Player))
        {
            foreach (var title in deck.GroupBy(
                         creation => facts.Title(creation.Faces[0]),
                         StringComparer.Ordinal))
            {
                long maximum = title
                    .Select(creation => facts.PrintedValue(
                        creation.Faces[0], "MaxPerDeck", players: 1))
                    .Where(value => value > 0)
                    .DefaultIfEmpty(long.MaxValue)
                    .Min();
                if (title.LongCount() > maximum)
                {
                    throw new ArgumentException(
                        $"player {deck.Key}'s deck contains {title.LongCount()} copies of "
                        + $"'{title.Key}', whose printed maximum is {maximum}",
                        nameof(dealt));
                }
            }
        }
    }

    private static string? DeckOf(Creation creation) => creation.Source switch
    {
        CreationSource.HeroDeck or CreationSource.PlayerDeck =>
            $"player:{creation.Player}",
        CreationSource.Encounter or CreationSource.EncounterSet => "encounter",
        CreationSource.MainScheme => "main-scheme",
        CreationSource.Villain => "villain",
        _ => null,
    };

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
