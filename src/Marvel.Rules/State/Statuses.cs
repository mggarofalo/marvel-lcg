namespace Marvel.Rules.State;


/// <summary>
/// Status cards: tough, stunned, confused.
/// </summary>
/// <remarks>
/// <para>
/// <b>A status is a card, not a flag.</b> The recorded milestone board is
/// unambiguous: when Rhino gains Tough, a new card appears with its own object
/// id, in <c>StatusArea</c>, attached to Rhino — and Rhino's own
/// <c>toughness</c> field stays at zero. Modelling it as a counter on the
/// villain would produce a board with the right behaviour and the wrong digest.
/// </para>
/// <para>
/// It is also the first card the engine makes after setup, so it is where the
/// append-only id contract is first tested by something other than dealing:
/// the milestone board deals 81 cards, ids 0..80, and the Tough is 81.
/// </para>
/// </remarks>
public static class Statuses
{
    /// <summary>The Tough status card's printed id.</summary>
    public const string Tough = "tough";

    /// <summary>The Stunned status card's printed id. <c>rr:stun-stunned</c>.</summary>
    public const string Stunned = "stunned";

    /// <summary>The Confused status card's printed id. <c>rr:confuse-confused</c>.</summary>
    public const string Confused = "confused";

    /// <summary>How many of a status a card carries.</summary>
    /// <remarks>
    /// A count rather than a flag, because <c>rr:steady</c> turns on the
    /// difference between one and two — "that character is not stunned unless
    /// they have two stunned status cards".
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="host">The card that might carry them.</param>
    /// <param name="status">The status's printed id.</param>
    public static int Count(World world, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);

        int held = 0;
        foreach (var area in world.Areas)
        {
            if (area.Type != DeckType.StatusArea || area.Host != host.ObjectId)
            {
                continue;
            }

            held += area.Cards.Count(card => card.FaceId == status);
        }

        return held;
    }

    /// <summary>Every status card of a kind on a card.</summary>
    /// <param name="world">The world.</param>
    /// <param name="host">The card carrying them.</param>
    /// <param name="status">The status's printed id.</param>
    public static IReadOnlyList<Card> On(World world, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);

        var found = new List<Card>();
        foreach (var area in world.Areas)
        {
            if (area.Type == DeckType.StatusArea && area.Host == host.ObjectId)
            {
                found.AddRange(area.Cards.Where(card => card.FaceId == status));
            }
        }

        return found;
    }

    /// <summary>
    /// How many of a status a card may carry — <c>rr:status-cards.1</c>.
    /// </summary>
    /// <remarks>
    /// "A character cannot have more than one status card of each type at a
    /// time", and <c>.1.1</c>: "characters with the steady keyword can have one
    /// additional confused status card and one additional stunned status card."
    /// <para>
    /// <c>rr:stalwart.1</c> is the other end: a stalwart character "cannot have
    /// confused or stunned status cards" at all. Tough is not one of the two
    /// and is unaffected by either keyword.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="host">The card.</param>
    /// <param name="status">The status's printed id.</param>
    public static int Limit(World world, ICardFacts facts, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(host);

        // `rr:status-cards.1` caps **every** type: "a character cannot have more
        // than one status card of each type at a time." Tough included --
        // `rr:tough.2.1` describes what happens to a character that has more
        // than one, which is a state a card ability can create by saying so,
        // not one this default permits.
        //
        // The two keywords move the cap and only for the two statuses they
        // name; neither says anything about tough.
        if (status is not (Stunned or Confused))
        {
            return 1;
        }

        if (StateFields.Modified(world, host, "stalwart", facts, world.Players) > 0)
        {
            return 0;
        }

        // `rr:status-cards.1.1`: "characters with the steady keyword can have
        // one additional confused status card and one additional stunned status
        // card."
        return StateFields.Modified(world, host, "steady", facts, world.Players) > 0 ? 2 : 1;
    }

    /// <summary>
    /// Whether a character actually <i>is</i> stunned or confused —
    /// <c>rr:stun-stunned.3</c>, <c>rr:confuse-confused.3</c>.
    /// </summary>
    /// <remarks>
    /// "A character is stunned if it has a stunned status card", and
    /// <c>.3.1</c>: "a character with the steady keyword is stunned <b>only if
    /// it has two</b>". So carrying one and being it are different questions
    /// the moment steady is in play.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="host">The card.</param>
    /// <param name="status">The status's printed id.</param>
    public static bool Afflicted(World world, ICardFacts facts, Card host, string status) =>
        Count(world, host, status) >= Limit(world, facts, host, status)
        && Limit(world, facts, host, status) > 0;

    /// <summary>Whether a card already carries a status.</summary>
    /// <param name="world">The world.</param>
    /// <param name="host">The card that might carry it.</param>
    /// <param name="status">The status's printed id.</param>
    public static bool Has(World world, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);

        foreach (var area in world.Areas)
        {
            if (area.Type != DeckType.StatusArea || area.Host != host.ObjectId)
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                if (card.FaceId == status)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Gives a card a status.</summary>
    /// <remarks>
    /// The status area belongs to the scenario however it was caused, which is
    /// why the recorded Tough on Rhino has owner -1: a card takes the owner of
    /// the place it is made in, and this place is the villain's.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="host">The card gaining it.</param>
    /// <param name="status">The status's printed id.</param>
    /// <returns>The new card.</returns>
    public static Card Give(World world, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);

        var area = world.AreaOf(
            DeckType.StatusArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        return world.CreateCard(status, area);
    }

    /// <summary>
    /// Gives a status if the character can take one — <c>rr:status-cards.1</c>.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="host">The card gaining it.</param>
    /// <param name="status">The status's printed id.</param>
    /// <returns>The new card, or null when it could not take one.</returns>
    public static Card? Inflict(World world, ICardFacts facts, Card host, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(host);

        return Count(world, host, status) < Limit(world, facts, host, status)
            ? Give(world, host, status)
            : null;
    }

    /// <summary>Remove the statuses forbidden by newly gained stalwart.</summary>
    /// <remarks>
    /// <c>rr:stalwart.2</c>: "If a character gains the stalwart keyword while
    /// they have a stunned and/or confused status card, each stunned and/or
    /// confused status card is removed from that character." This is a
    /// transition rule, not merely the zero limit returned by <see cref="Limit"/>.
    /// </remarks>
    public static void RemoveAfflictionsIfStalwart(
        World world, ICardFacts facts, Card host, string trigger,
        List<Events.GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);

        if (StateFields.Modified(world, host, "stalwart", facts, world.Players) <= 0)
        {
            return;
        }

        foreach (var status in On(world, host, Stunned)
                     .Concat(On(world, host, Confused))
                     .OrderBy(card => card.ObjectId)
                     .ToList())
        {
            Play.Discard.Card(world, status, trigger, events);
        }
    }

    /// <summary>Apply <c>rr:stalwart.2</c> to every character on the board.</summary>
    public static void RemoveAfflictionsIfStalwart(
        World world, ICardFacts facts, string trigger, List<Events.GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);

        foreach (var character in world.Cards
                     .Where(card => DeckTypes.IsInPlay(card.Area.Type)
                         && FacedownDrones.Kind(card, facts) is CardKind.Hero
                             or CardKind.AlterEgo or CardKind.Ally or CardKind.Minion
                             or CardKind.EncounterVillain)
                     .OrderBy(card => card.ObjectId))
        {
            RemoveAfflictionsIfStalwart(
                world, facts, character, trigger, events);
        }
    }

    /// <summary>
    /// Whether a character is discarded for becoming stunned or confused —
    /// <c>rr:vulnerable</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "<b>Forced Interrupt</b>: when this character becomes confused or
    /// stunned, discard it." <c>rr:vulnerable.2</c> is emphatic that it is a
    /// discard and not a defeat — "it is discarded before the damage is applied
    /// and <b>is not considered defeated</b>" — so no "When Defeated" ability
    /// fires and nothing goes to the victory display.
    /// </para>
    /// <para>
    /// <c>rr:vulnerable.3</c>: "if a character has both the steady and
    /// vulnerable keywords, the vulnerable keyword does not take effect until
    /// that character has two confused or two stunned status cards" — which is
    /// <see cref="Afflicted"/>, so this asks that rather than counting cards.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="host">The character.</param>
    public static bool Vulnerable(World world, ICardFacts facts, Card host)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(host);

        return StateFields.Modified(world, host, "vulnerable", facts, world.Players) > 0
            && (Afflicted(world, facts, host, Stunned)
                || Afflicted(world, facts, host, Confused));
    }
}
