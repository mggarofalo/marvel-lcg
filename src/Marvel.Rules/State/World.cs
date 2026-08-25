using Marvel.Core.Digest;

namespace Marvel.Rules.State;

/// <summary>
/// Every card in a game, and every place they can be.
/// </summary>
/// <remarks>
/// <para>
/// The board: the first argument whenever the game resolves a decision. Cards are held in a flat list indexed by
/// <c>object_id</c>, which is also their creation order — the id allocator is a
/// counter and ids are never reused, so the list is append-only and
/// <c>cards[i].ObjectId == i</c> always.
/// </para>
/// <para>
/// <b>Nothing is ever removed from it.</b> A card removed from the game moves to
/// the removed area and is still recorded, so the set of ids in a digest is
/// always <c>0..highest</c>. Dropping one would renumber nothing but would make
/// the digest disagree.
/// </para>
/// </remarks>
public sealed class World
{
    private readonly List<Card> cards = [];
    private readonly List<Area> areas = [];
    private readonly List<Seat> seats = [];
    private readonly List<GameArea> gameAreas = [];
    private readonly ICardFacts facts;

    /// <summary>Creates an empty world.</summary>
    /// <param name="facts">The printed card data this game is played with.</param>
    /// <param name="players">How many players are in the game.</param>
    public World(ICardFacts facts, int players)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        this.facts = facts;
        Players = players;
        Effects = new Timing.ContinuousEffects(this);
        Windows = new Timing.Windows(this);
        Agenda = new Play.Agenda();

        // An ordinary game has exactly one game area holding every play area,
        // and nothing in the rules distinguishes that from having none. Making
        // it here rather than lazily means every predicate about reach has the
        // same shape whether or not a scenario ever splits.
        var whole = CreateGameArea();
        whole.Add(PlayArea.Villains);
        for (int seat = 0; seat < players; seat++)
        {
            whole.Add(PlayArea.Of(seat));
        }
    }

    /// <summary>
    /// Everything continuously in force: constant abilities, lasting effects
    /// and delayed effects.
    /// </summary>
    /// <remarks>
    /// Part of the board rather than of the engine, because a lasting effect
    /// outlives the turn that made it and has to be saved with the game. See
    /// <c>docs/timing.md</c>.
    /// </remarks>
    public Timing.ContinuousEffects Effects { get; }

    /// <summary>
    /// Where the game is when it is part-way through resolving something: the
    /// stack of open interrupt and response windows.
    /// </summary>
    /// <remarks>
    /// On the board because a half-resolved occurrence has to be saved with the
    /// game. See <c>docs/timing.md</c>.
    /// </remarks>
    public Timing.Windows Windows { get; }

    /// <summary>
    /// What the game still has to do, and where in it the game is.
    /// </summary>
    /// <remarks>
    /// A phase is a list of steps on the board rather than a call, so that the
    /// game can stop in the middle of one. See <c>docs/timing.md</c>.
    /// </remarks>
    public Play.Agenda Agenda { get; }

    /// <summary>The seat value meaning "the scenario", not a player.</summary>
    public const int Scenario = -1;

    /// <summary>How many players are in the game.</summary>
    public int Players { get; }

    /// <summary>Every card, ascending by <see cref="Card.ObjectId"/>.</summary>
    public IReadOnlyList<Card> Cards => cards;

    /// <summary>Every area, in the order they were made.</summary>
    public IReadOnlyList<Area> Areas => areas;

    /// <summary>The players, in seat order.</summary>
    public IReadOnlyList<Seat> Seats => seats;

    /// <summary>Every game area, in the order they were made.</summary>
    /// <remarks>
    /// Never empty: the first is made with the world and holds every play area.
    /// A scenario that splits adds more; see <see cref="GameArea"/>.
    /// </remarks>
    public IReadOnlyList<GameArea> GameAreas => gameAreas;

    /// <summary>
    /// The enemy attack being resolved, or <c>null</c> when none is.
    /// </summary>
    /// <remarks>
    /// On the board because an attack spans several steps of the agenda and a
    /// player is asked a question in the middle of it. See
    /// <see cref="EnemyAttack"/>.
    /// </remarks>
    public EnemyAttack? Attack { get; set; }

    /// <summary>Whether the game has ended.</summary>
    /// <remarks>
    /// The engine answers a <c>null</c> prompt once this is set, which is the only
    /// thing that makes a prompt absent. Nothing is asked of a player after a
    /// game is over.
    /// </remarks>
    public bool IsOver { get; set; }

    /// <summary>The seat holding the first player token.</summary>
    /// <remarks>
    /// Passed clockwise at the end of every villain phase
    /// (<c>rr:villain-phase.5</c>). It reaches the digest as
    /// <c>k_first_player_token</c> on that seat's identity, so moving it is a
    /// board change even though no card moves.
    /// </remarks>
    public int FirstPlayer { get; set; }

    /// <summary>Makes an empty game area.</summary>
    /// <remarks>
    /// Empty on purpose. Kang's stage 3A says "create your own game area and
    /// place this scheme in it", so creating and populating are two steps, and
    /// God of Lies keeps a game area with no players in it at all.
    /// </remarks>
    public GameArea CreateGameArea()
    {
        var area = new GameArea(gameAreas.Count);
        gameAreas.Add(area);
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
    /// play area rather than stored on the card. PR #115 modelled a Kang split
    /// as 47 cards changing a tag and was reverted for it.
    /// </para>
    /// <para>
    /// <b>The engine cannot yet tell a client this happened.</b> The event
    /// vocabulary is the set that explains every state change in the frozen
    /// corpus, and a game area is invisible to the digest the corpus is made of
    /// (MARVEL-174) — so this change is emittable but not derivable, and no
    /// existing event kind covers it. Raised on MARVEL-175 rather than settled
    /// here, because <c>tools/events/model.py</c> already records the opposite
    /// decision in as many words.
    /// </para>
    /// </remarks>
    /// <param name="area">The play area that is moving.</param>
    /// <param name="destination">The game area it joins.</param>
    public void Join(PlayArea area, GameArea destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!gameAreas.Contains(destination))
        {
            throw new ArgumentException("that game area is not in this world", nameof(destination));
        }

        foreach (var existing in gameAreas)
        {
            existing.Remove(area);
        }

        destination.Add(area);
    }

    /// <summary>Takes a play area out of every game area.</summary>
    /// <remarks>
    /// Kang's stage 2B "remains in play in a central location […] though it is
    /// not part of any other game area", and its text stays active for everyone.
    /// Being in no game area is a real placement with a rules consequence, not
    /// an error state — see <c>Places.CanAffect</c>.
    /// </remarks>
    /// <param name="area">The play area to detach.</param>
    public void Detach(PlayArea area)
    {
        foreach (var existing in gameAreas)
        {
            existing.Remove(area);
        }
    }

    /// <summary>Which game area a play area is in, or <c>null</c> when it is in none.</summary>
    /// <param name="area">The play area.</param>
    public GameArea? GameAreaOf(PlayArea area) =>
        gameAreas.FirstOrDefault(candidate => candidate.Contains(area));

    /// <summary>Makes a seat and the areas that belong to it.</summary>
    /// <param name="name">The player's name, e.g. <c>Spider-Man</c>.</param>
    /// <remarks>
    /// The five areas are made here in the order the Python engine makes them,
    /// which is why this is one call rather than five. Area ids are not on the
    /// wire, but keeping the order lets a divergence be read against the
    /// engine's own log.
    /// </remarks>
    public Seat CreateSeat(string name)
    {
        int index = seats.Count;
        var seat = new Seat(
            index,
            name,
            identity: CreateArea(DeckType.AsideDeck, index, PlayArea.Of(index)),
            // The nemesis pile is the player's place and the scenario's
            // property, so a card made in it is owned by the scenario. The
            // recorded digest is unambiguous: an obligation sitting in a
            // seat's pile records owner -1.
            nemesis: CreateArea(DeckType.AsideDeck, Scenario, PlayArea.Of(index)),
            deck: CreateArea(DeckType.PlayerDeck, index, PlayArea.Of(index)),
            hand: CreateArea(DeckType.HandsArea, index, PlayArea.Of(index)),
            hero: CreateArea(DeckType.HeroArea, index, PlayArea.Of(index)));
        seats.Add(seat);
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
    /// <param name="type">What kind of place it is.</param>
    /// <param name="playArea">Which play area it sits in. Defaults to the villain's.</param>
    /// <param name="host">The card it hangs off, or -1.</param>
    /// <param name="cardOwner">Who a card made here belongs to, or -1.</param>
    public Area AreaOf(
        DeckType type, PlayArea? playArea = null, int host = -1, int cardOwner = Scenario)
    {
        var where = playArea ?? PlayArea.Villains;
        foreach (var area in areas)
        {
            if (area.Type == type && area.PlayArea == where && area.Host == host)
            {
                return area;
            }
        }

        return CreateArea(type, cardOwner, where, host);
    }

    /// <summary>The one card in an area of this type, or null.</summary>
    /// <param name="type">What kind of place to look in.</param>
    public Card? TheCardIn(DeckType type)
    {
        foreach (var area in areas)
        {
            if (area.Type == type && area.Cards.Count > 0)
            {
                return area.Cards[0];
            }
        }

        return null;
    }

    /// <summary>Makes an area.</summary>
    /// <param name="type">What kind of place it is.</param>
    /// <param name="cardOwner">Who a card made here belongs to, or -1 for the scenario.</param>
    /// <param name="playArea">Which play area it sits in. Defaults to the villain's.</param>
    /// <param name="host">The card it is bound to, or -1.</param>
    public Area CreateArea(
        DeckType type, int cardOwner = -1, PlayArea? playArea = null, int host = -1)
    {
        var area = new Area(areas.Count, type, cardOwner, playArea ?? PlayArea.Villains, host);
        areas.Add(area);
        return area;
    }

    /// <summary>Makes a card and puts it in an area.</summary>
    /// <remarks>
    /// The id is the next one, so the order these calls are made in <b>is</b> the
    /// wire format. See <c>Marvel.Content.Setup.Dealer</c>.
    /// </remarks>
    /// <param name="spec">Comma-separated face ids. One card, however many faces.</param>
    /// <param name="into">Where it starts. Its owner becomes the card's owner.</param>
    public Card CreateCard(string spec, Area into)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(into);

        // The engine's rule: a card belongs to whoever owns the place it was
        // made in, falling back to the scenario. Not to the seat that asked for
        // it -- an obligation is dealt for a player and owned by the scenario.
        var card = new Card(cards.Count, spec.Split(','), into.CardOwner);
        cards.Add(card);
        into.Append(card);
        return card;
    }

    /// <summary>Moves a card to the end (the top) of an area.</summary>
    /// <param name="card">The card.</param>
    /// <param name="destination">Where it goes.</param>
    public static void MoveToTop(Card card, Area destination)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(destination);
        card.Area.Remove(card);
        destination.Append(card);
    }

    /// <summary>The state digest of this world.</summary>
    /// <remarks>
    /// One record per card, ascending by id, nothing excluded — not the rules
    /// pseudo-card, not id 0, not the middle of a deck.
    /// </remarks>
    public StateDigest Digest()
    {
        var positions = new Dictionary<int, (string Zone, int Index)>();
        foreach (var area in areas)
        {
            string zone = area.Type.ToString();
            for (int index = 0; index < area.Cards.Count; index++)
            {
                positions[area.Cards[index].ObjectId] = (zone, index);
            }

            for (int index = 0; index < area.Removed.Count; index++)
            {
                positions[area.Removed[index].ObjectId] = (zone + "/removed", index);
            }
        }

        var records = new List<CardRecord>(cards.Count);
        foreach (var card in cards)
        {
            // `/absent` should not happen. It is emitted rather than raised
            // because an oracle that can crash while computing itself is worse
            // than one with a visible anomaly.
            var (zone, index) = positions.TryGetValue(card.ObjectId, out var found)
                ? found
                : (card.Area.Type + "/absent", -1);

            bool inPlay = DeckTypes.IsInPlay(card.Area.Type);
            records.Add(new CardRecord(
                Id: card.ObjectId,
                Card: card.FaceId,
                Zone: zone,
                Owner: card.Owner,
                Index: index,
                Host: card.Area.Host,
                FaceUp: card.FaceUp,
                Fields: StateFields.For(
                    card, facts, Players, inPlay, card.HasRegisteredTokens,
                    hasFirstPlayerToken: card.Owner == FirstPlayer
                                         && card.Area.Type == DeckType.HeroArea,
                    world: this)));
        }

        return new StateDigest(records);
    }
}
