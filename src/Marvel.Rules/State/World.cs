using Marvel.Core.Digest;
using Marvel.Core.Random;

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
    /// <param name="seed">The game's seed.</param>
    public World(ICardFacts facts, int players, uint seed = 0)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        this.facts = facts;
        Players = players;
        Random = new EngineRandom(seed);
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
    /// The enemy activation being resolved, of either kind, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The umbrella over <see cref="Attack"/> and a scheme activation, which
    /// had no value of its own. See <see cref="EnemyActivation"/>.
    /// </remarks>
    public EnemyActivation? Activation { get; set; }

    /// <summary>
    /// The activation whose attack or scheme windows have finished, while its
    /// initiating effect is being resumed.
    /// </summary>
    /// <remarks>
    /// <c>rr:activation.7</c> makes the effect wait until the activation has
    /// fully resolved. The result is captured when the attack or scheme ends,
    /// remains available through that occurrence's response window, and is
    /// cleared after the completion sentinel resumes waiting card effects.
    /// </remarks>
    public EnemyActivation? FinishedActivation { get; set; }

    /// <summary>
    /// The enemy attack being resolved, or <c>null</c> when none is.
    /// </summary>
    /// <remarks>
    /// On the board because an attack spans several steps of the agenda and a
    /// player is asked a question in the middle of it. See
    /// <see cref="EnemyAttack"/>.
    /// </remarks>
    public EnemyAttack? Attack { get; set; }

    /// <summary>
    /// Extra hero seats added while the current attack's initiation interrupt
    /// window is open, before its attack state exists.
    /// </summary>
    public IReadOnlyList<int> PendingAdditionalAttackPlayers { get; set; } = [];

    /// <summary>
    /// The enemy attack that just finished, or <c>null</c> outside the window
    /// that follows one.
    /// </summary>
    /// <remarks>
    /// <c>rr:attack-enemy-activation.step.6.a</c> lists four printed trigger
    /// shapes that all reason about an attack <i>after</i> it is over — "after
    /// [character] attacks <b>and damages/defeats</b> [you/an ally]", "after
    /// [character] is attacked", "after [character] defends <b>and takes no
    /// damage</b>", "after [character] [takes/deals] damage". Each of them
    /// needs a fact about the attack that <see cref="Attack"/> no longer holds:
    /// the attack is cleared when it ends, and the abilities in question run in
    /// the window after that.
    /// </remarks>
    public EnemyAttack? FinishedAttack { get; set; }

    /// <summary>
    /// The character attack being resolved, or <c>null</c> when none is.
    /// </summary>
    /// <remarks>
    /// The player's half of <see cref="Attack"/>, and separate for the same
    /// reason: <c>rr:attack-player-ability-type.step.7</c> puts abilities
    /// around a character's attack — "after [character] attacks [and
    /// damages/defeats] [an enemy/a minion]", "after [character] is attacked"
    /// — and an ability may ask the player something, so the attack spans more
    /// than one turn of the loop.
    /// <para>
    /// <b>Who attacked, not just which seat.</b> <c>rr:ally.2</c> lets a player
    /// attack with an ally, and <c>rr:you-your.15</c> is emphatic that an
    /// ally's attack is <b>not</b> performed by that player's identity — so a
    /// card that acts on "the attacking character" needs the character.
    /// </para>
    /// </remarks>
    public CharacterAttack? CharacterAttack { get; set; }

    /// <summary>
    /// The character thwart being resolved, or <c>null</c> when none is.
    /// </summary>
    /// <remarks>
    /// The thwart's half of <see cref="CharacterAttack"/>. It spans more than
    /// one turn of the loop for the same reason: <c>rr:consequential-damage.1</c>
    /// puts an ally's consequential damage after "abilities that are triggered
    /// by the ally attacking <b>or thwarting</b>", so a window sits between the
    /// power being used and the ally taking its damage.
    /// </remarks>
    public CharacterThwart? CharacterThwart { get; set; }


    /// <summary>Whether the game has ended.</summary>
    /// <remarks>
    /// The engine answers a <c>null</c> prompt once this is set, which is the only
    /// thing that makes a prompt absent. Nothing is asked of a player after a
    /// game is over.
    /// </remarks>
    public bool IsOver => Result is not Play.Outcome.Unfinished;

    /// <summary>
    /// How the game ended, or that it has not.
    /// </summary>
    /// <remarks>
    /// The rules name two endings and they are not the same fact.
    /// <c>rr:main-scheme-main-scheme-deck.2.1</c>: "if the villain completes the
    /// final stage of the main scheme deck, <b>the villain wins the game</b>."
    /// <c>rr:villain-defeat</c>: "if the final stage of the villain deck is
    /// defeated, <b>the players win the game</b>." A boolean can say a game is
    /// over and cannot say which of those happened — which is the one thing a
    /// player wants to know.
    /// </remarks>
    public Play.Outcome Result { get; private set; }

    /// <summary>Ends the game.</summary>
    /// <remarks>
    /// Once, and it cannot be un-ended. Two endings racing would mean a rule
    /// resolved after the game stopped, which is a fault rather than a tie.
    /// </remarks>
    /// <param name="outcome">Who won.</param>
    public void Finish(Play.Outcome outcome)
    {
        if (Result is not Play.Outcome.Unfinished)
        {
            throw new Play.RulesNotImplementedException(
                $"the game already ended as {Result} and cannot also end as {outcome}");
        }

        Result = outcome;
    }

    /// <summary>The seat holding the first player token.</summary>
    /// <remarks>
    /// Passed clockwise at the end of every villain phase
    /// (<c>rr:villain-phase.5</c>). It reaches the digest as
    /// <c>k_first_player_token</c> on that seat's identity, so moving it is a
    /// board change even though no card moves.
    /// </remarks>
    public int FirstPlayer { get; set; }

    /// <summary>
    /// The printed card data this game is played with.
    /// </summary>
    /// <remarks>
    /// The same object the constructor was given, and the one the digest is
    /// already built from. Exposed because a caller holding a <see cref="World"/>
    /// is by definition in that game: threading a second <c>ICardFacts</c>
    /// alongside it is how two of them get to disagree.
    /// <para>
    /// The rules still take it as a parameter where they can — that keeps a
    /// function's inputs visible in its signature. This is for the callers that
    /// have a world and nothing else, which is what <c>ICardAbilities</c> hands
    /// a card.
    /// </para>
    /// </remarks>
    public ICardFacts Facts => facts;

    /// <summary>
    /// What the cards in this game do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Here for the same reason <see cref="Facts"/> is, and with the same
    /// bargain: the rules take <c>ICardAbilities</c> as a parameter wherever
    /// they can, because that keeps a function's inputs visible in its
    /// signature. This is for the callers that have a world and nothing else.
    /// </para>
    /// <para>
    /// <b>Defeat is the caller that needed it.</b>
    /// <c>rr:when-defeated-abilities</c> resolves a card's ability before the
    /// card leaves play, and a defeat happens inside <c>Damage.Deal</c> —
    /// four calls below anything that was ever handed an
    /// <c>ICardAbilities</c>. Threading one down that path would put it in
    /// seventeen signatures that have no use for it.
    /// </para>
    /// <para>
    /// Defaults to <see cref="Play.NoCardAbilities"/>, so a board built by hand
    /// is a board where no card does anything — which is what a board built by
    /// hand is.
    /// </para>
    /// </remarks>
    public Play.ICardAbilities Abilities { get; set; } = new Play.NoCardAbilities();

    /// <summary>
    /// Whether this game is being played in expert mode —
    /// <c>rr:modes-of-play</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Expert Mode is a modification of standard mode for advanced players who
    /// seek a greater challenge", and <c>.2</c> says what it changes: the
    /// listed expert villain stages, and the Expert encounter set added to the
    /// deck. Both of those are the dealer's business and it already does them —
    /// the <c>_expert</c> campaigns list different stages and sets. What was
    /// missing is that <b>86 cards in the pool read the mode</b>, and a board
    /// that did not carry it could not answer them.
    /// </para>
    /// <para>
    /// <b>One flag and not the four modes.</b> <c>rr:modes-of-play</c> names
    /// expert, heroic, skirmish and campaign, and <c>.3</c> lets them combine —
    /// so this is deliberately not an enum. It is also not a set: heroic mode
    /// carries a level number rather than a flag (<c>.4</c>, "deal X additional
    /// encounter cards [...] where X is equal to the chosen heroic level"), and
    /// modelling it as a member of a set would get it wrong. The other three
    /// arrive when a card reads them.
    /// </para>
    /// </remarks>
    public bool Expert { get; set; }

    /// <summary>
    /// The game's one random stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>On the board rather than in the dealer, because the game keeps
    /// drawing from it.</b> Non-negotiable 2 in <c>AGENTS.md</c> is "one
    /// MT19937 stream, seeded once per game": setup's two shuffles are the
    /// first draws from it and <c>rr:player-deck.1</c>'s reshuffle is a later
    /// one. A second generator made mid-game would restart the stream and
    /// change every card drawn afterwards.
    /// </para>
    /// <para>
    /// Seeded at construction, so a world built by hand for a test has a
    /// deterministic stream too — seed 0 — rather than no stream at all.
    /// </para>
    /// </remarks>
    public EngineRandom Random { get; }

    /// <summary>
    /// The seats in player order — <c>rr:in-player-order</c>.
    /// </summary>
    /// <remarks>
    /// "The first player performs their part of the sequence first, followed by
    /// the other players in clockwise order." Clockwise is ascending seat index
    /// wrapping at the table, so this is the first player and then everyone
    /// else, and it moves when the first player token does.
    /// </remarks>
    public IEnumerable<int> PlayerOrder
    {
        get
        {
            for (int offset = 0; offset < Players; offset++)
            {
                int seat = (FirstPlayer + offset) % Players;

                // `rr:player-elimination.6`: "effects that refer to the players
                // in the game ignore eliminated players." The seat stays in the
                // list -- `Players` is the starting count and the per-player
                // icon still uses it -- but nothing takes a turn there.
                if (seats.Count > seat && !seats[seat].Eliminated)
                {
                    yield return seat;
                }
            }
        }
    }

    /// <summary>
    /// Shuffles a pile, drawing from the game's one stream.
    /// </summary>
    /// <remarks>
    /// <b>A pile of fewer than two cards is not shuffled at all</b>, and that
    /// is not an optimisation: there is nothing to shuffle, and calling through
    /// would consume a slot in the shared stream and desynchronise every draw
    /// after it.
    /// </remarks>
    /// <param name="area">The pile.</param>
    /// <returns>Whether Fisher-Yates ran and consumed the shared random stream.</returns>
    public bool Shuffle(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        if (area.Cards.Count < 2)
        {
            return false;
        }

        var order = area.Cards.ToList();
        Random.Shuffle(order);
        area.Replace(order);
        return true;
    }

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
    /// vocabulary is the set that explained every state change in a large
    /// sample of recorded play, and a game area is invisible to the digest
    /// (MARVEL-174) — so a sample drawn from digests could never have contained
    /// one. This change is emittable but not derivable, and no existing event
    /// kind covers it. Raised on MARVEL-175 rather than settled here.
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
    /// The six areas are made in one fixed order, which is why this is one
    /// call rather than five. Area ids are not on the wire, so the order does
    /// not have to be this one — but it does have to be the same every time,
    /// or two deals of a seed allocate ids differently.
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
            hero: CreateArea(DeckType.HeroArea, index, PlayArea.Of(index)),
            // Created last so the existing five area identities remain stable.
            // It is in the player's play area but scenario-owned until a rule
            // such as Linked transfers ownership of one of its cards.
            setAside: CreateArea(DeckType.AsideDeck, Scenario, PlayArea.Of(index)));
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

        // `rr:removed-from-the-game.2`: this is a terminal game state, not a
        // set-aside pile. Effects that may later retrieve a card use
        // AsideDeck instead.
        if (card.Area.Type == DeckType.RemovedArea)
        {
            throw new InvalidOperationException(
                $"card {card.ObjectId} was removed from the game and cannot reenter it");
        }

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
            // because a digest that can crash while computing itself is worse
            // than one with a visible anomaly in it.
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
