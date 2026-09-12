using Marvel.Rules.Events;
using Marvel.Rules.Play;

namespace Marvel.Rules.State;

/// <summary>Creates and changes the deterministic topology of a world.</summary>
public static class WorldTopology
{
    /// <summary>
    /// Shuffles a pile, drawing from the game's one stream.
    /// </summary>
    /// <remarks>
    /// <b>A pile of fewer than two cards is not shuffled at all</b>, and that
    /// is not an optimisation: there is nothing to shuffle, and calling through
    /// would consume a slot in the shared stream and desynchronise every draw
    /// after it.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="area">The pile.</param>
    /// <returns>Whether Fisher-Yates ran and consumed the shared random stream.</returns>
    public static bool Shuffle(this World world, Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        if (area.Cards.Count < 2)
        {
            return false;
        }

        var order = area.Cards.ToList();
        world.Random.Shuffle(order);
        area.Replace(order);
        return true;
    }

    /// <summary>Makes an empty game area.</summary>
    /// <remarks>
    /// Empty on purpose. Kang's stage 3A says "create your own game area and
    /// place this scheme in it", so creating and populating are two steps, and
    /// God of Lies keeps a game area with no players in it at all.
    /// </remarks>
    public static GameArea CreateGameArea(this World world)
    {
        var area = new GameArea(world._gameAreas.Count);
        world._gameAreas.Add(area);
        return area;
    }

    /// <summary>Moves a play area into a game area, leaving whichever held it.</summary>
    /// <remarks>
    /// <para>
    /// <c>pack:mc11:game-areas</c>: "choose a game area and reorient the cards
    /// on the table to indicate that you have joined that game area."
    /// </para>
    /// <para>
    /// <b>One operation, not one per card.</b> A play area moves and every card
    /// in it comes along, because a card's game area is looked up through its
    /// play area rather than stored on the card.
    /// </para>
    /// <para>
    /// <b>This event is emitted, not derived.</b> A game area is invisible to
    /// the v2 digest (the original investigation), so a before/after comparison can never find
    /// the change. This method knows it performed the join and emits one
    /// <see cref="PlayAreaJoined"/> for it.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="area">The play area that is moving.</param>
    /// <param name="destination">The game area it joins.</param>
    /// <param name="trigger">The timing point that caused the join.</param>
    /// <param name="events">The event stream to append to.</param>
    public static void Join(this World world,
        PlayArea area, GameArea destination, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);
        if (!world._gameAreas.Contains(destination))
        {
            throw new ArgumentException("that game area is not in this world", nameof(destination));
        }

        if (destination.Contains(area))
        {
            return;
        }

        foreach (var existing in world._gameAreas)
        {
            existing.Remove(area);
        }

        destination.Add(area);
        events.Add(new PlayAreaJoined(area.Player, destination.Id)
        {
            Trigger = trigger,
            Verb = "Join",
        });
    }

    /// <summary>Takes a play area out of its game area.</summary>
    /// <remarks>
    /// Kang's stage 2B "remains in play in a central location […] though it is
    /// not part of any other game area", and its text stays active for everyone.
    /// Being in no game area is a real placement with a rules consequence, not
    /// an error state — see <c>Places.CanAffect</c>.
    /// <para>
    /// The topology change is invisible to the v2 digest, so this emits one
    /// <see cref="PlayAreaDetached"/> naming the membership that was removed.
    /// A play area already outside every game area is unchanged and silent.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="area">The play area to detach.</param>
    /// <param name="trigger">The timing point that caused the detachment.</param>
    /// <param name="events">The event stream to append to.</param>
    public static void Detach(this World world, PlayArea area, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);

        var existing = world.GameAreaOf(area);
        if (existing is null)
        {
            return;
        }

        if (!existing.Remove(area))
        {
            throw new InvalidOperationException(
                $"play area {area} was not in its resolved game area");
        }

        events.Add(new PlayAreaDetached(area.Player, existing.Id)
        {
            Trigger = trigger,
            Verb = "Detach",
        });
    }

    /// <summary>Which game area a play area is in, or <c>null</c> when it is in none.</summary>
    /// <param name="world">The world.</param>
    /// <param name="area">The play area.</param>
    public static GameArea? GameAreaOf(this World world, PlayArea area) =>
        world._gameAreas.FirstOrDefault(candidate => candidate.Contains(area));

    /// <summary>Makes a seat and the areas that belong to it.</summary>
    /// <param name="world">The world.</param>
    /// <param name="name">The player's name, e.g. <c>Spider-Man</c>.</param>
    /// <remarks>
    /// The six areas are made in one fixed order, which is why this is one
    /// call rather than five. Area ids are not on the wire, so the order does
    /// not have to be this one — but it does have to be the same every time,
    /// or two deals of a seed allocate ids differently.
    /// </remarks>
    public static Seat CreateSeat(this World world, string name)
    {
        int index = world._seats.Count;
        var seat = new Seat(
            index,
            name,
            identity: world.CreateArea(DeckType.AsideDeck, index, PlayArea.Of(index)),
            // The nemesis pile is the player's place and the scenario's
            // property, so a card made in it is owned by the scenario. The
            // recorded digest is unambiguous: an obligation sitting in a
            // seat's pile records owner -1.
            nemesis: world.CreateArea(DeckType.AsideDeck, World.Scenario, PlayArea.Of(index)),
            deck: world.CreateArea(DeckType.PlayerDeck, index, PlayArea.Of(index)),
            hand: world.CreateArea(DeckType.HandsArea, index, PlayArea.Of(index)),
            hero: world.CreateArea(DeckType.HeroArea, index, PlayArea.Of(index)),
            // Created last so the existing five area identities remain stable.
            // It is in the player's play area but scenario-owned until a rule
            // such as Linked transfers ownership of one of its cards.
            setAside: world.CreateArea(DeckType.AsideDeck, World.Scenario, PlayArea.Of(index)));
        world._seats.Add(seat);
        return seat;
    }

    /// <summary>Finds the area matching a place, making it if there is none.</summary>
    /// <remarks>
    /// <para>
    /// Areas appear during a game — an encounter discard pile the first time
    /// something is discarded, a status area the first time a card gains a
    /// status — so the engine needs to name a place before it necessarily exists.
    /// </para>
    /// <para>
    /// Safe to find-or-create because an area's identity is not on the wire:
    /// the digest records a card's <i>zone name</i>, index and host, none of
    /// which move when an area is made earlier or later. <c>AreaRef.Id</c> does
    /// carry it, and an event stream built across a session where an area was
    /// created at a different moment would number them differently — which is
    /// the same session-scoped-handle rule that governs affordance ids.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="type">What kind of place it is.</param>
    /// <param name="playArea">Which play area it sits in. Defaults to the villain's.</param>
    /// <param name="host">The card it hangs off, or -1.</param>
    /// <param name="cardOwner">Who a card made here belongs to, or -1.</param>
    public static Area AreaOf(this World world,
        DeckType type, PlayArea? playArea = null, int host = -1, int cardOwner = World.Scenario)
    {
        var where = playArea ?? PlayArea.Villains;
        foreach (var area in world._areas)
        {
            if (area.Type == type && area.PlayArea == where && area.Host == host)
            {
                return area;
            }
        }

        return world.CreateArea(type, cardOwner, where, host);
    }

    /// <summary>The one card in an area of this type, or null.</summary>
    /// <param name="world">The world.</param>
    /// <param name="type">What kind of place to look in.</param>
    public static Card? TheCardIn(this World world, DeckType type)
    {
        foreach (var area in world._areas)
        {
            if (area.Type == type && area.Cards.Count > 0)
            {
                return area.Cards[0];
            }
        }

        return null;
    }

    /// <summary>Makes an area.</summary>
    /// <param name="world">The world.</param>
    /// <param name="type">What kind of place it is.</param>
    /// <param name="cardOwner">Who a card made here belongs to, or -1 for the scenario.</param>
    /// <param name="playArea">Which play area it sits in. Defaults to the villain's.</param>
    /// <param name="host">The card it is bound to, or -1.</param>
    public static Area CreateArea(this World world,
        DeckType type, int cardOwner = -1, PlayArea? playArea = null, int host = -1)
    {
        Card? hostCard = null;
        if (host >= 0)
        {
            if (host >= world._cards.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(host), $"there is no host card {host}");
            }
            hostCard = world._cards[host];
        }

        var area = new Area(
            world._areas.Count, type, cardOwner, playArea ?? PlayArea.Villains, host, hostCard);
        world._areas.Add(area);
        return area;
    }

    /// <summary>Makes a card and puts it in an area.</summary>
    /// <remarks>
    /// The id is the next one, so the order these calls are made in <b>is</b> the
    /// wire format. See <c>Marvel.Content.Setup.Dealer</c>.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="spec">Comma-separated face ids. One card, however many faces.</param>
    /// <param name="into">Where it starts. Its owner becomes the card's owner.</param>
    public static Card CreateCard(this World world, string spec, Area into)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(into);
        into.ValidateCanAcceptCards();

        // The engine's rule: a card belongs to whoever owns the place it was
        // made in, falling back to the scenario. Not to the seat that asked for
        // it -- an obligation is dealt for a player and owned by the scenario.
        var card = new Card(world._cards.Count, spec.Split(','), into.CardOwner);
        world._cards.Add(card);
        into.Append(card);
        return card;
    }

}
