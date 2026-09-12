using Marvel.Core.Digest;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;

namespace Marvel.Rules.State;

/// <summary>
/// Every card in a game, and every place they can be.
/// </summary>
/// <remarks>
/// Cards are held in an append-only list indexed by <c>object_id</c>, which is
/// also their creation order: <c>Cards[i].ObjectId == i</c>. A card removed
/// from the game moves to the removed area and remains in this list, so a
/// digest always contains the complete range <c>0..highest</c>.
/// </remarks>
public sealed class World
{
    internal readonly List<Card> _cards = [];
    internal readonly List<Area> _areas = [];
    internal readonly List<Seat> _seats = [];
    internal readonly List<GameArea> _gameAreas = [];
    private readonly List<InformationSignal> informationSignals = [];
    private readonly ICardFacts facts;

    /// <summary>Records concealed knowledge without putting identities on the event wire.</summary>
    public void RecordInformation(InformationKind kind) =>
        informationSignals.Add(new InformationSignal(kind));

    internal void ClearInformationSignals() => informationSignals.Clear();

    internal IReadOnlyList<InformationSignal> TakeInformationSignals()
    {
        InformationSignal[] taken = [.. informationSignals];
        informationSignals.Clear();
        return taken;
    }

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
        var whole = this.CreateGameArea();
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
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>Every area, in the order they were made.</summary>
    public IReadOnlyList<Area> Areas => _areas;

    /// <summary>The players, in seat order.</summary>
    public IReadOnlyList<Seat> Seats => _seats;

    /// <summary>Every game area, in the order they were made.</summary>
    /// <remarks>
    /// Never empty: the first is made with the world and holds every play area.
    /// A scenario that splits adds more; see <see cref="GameArea"/>.
    /// </remarks>
    public IReadOnlyList<GameArea> GameAreas => _gameAreas;

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
    /// </remarks>
    public ICardFacts Facts => facts;

    /// <summary>
    /// What the cards in this game do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The public aggregate is retained for compatibility at construction and
    /// direct engine entry points. Rules procedures read the typed capability
    /// properties below, so each consumer sees only the card behavior it uses.
    /// </para>
    /// <para>
    /// <b>Defeat is the caller that needed it.</b>
    /// <c>rr:when-defeated-abilities</c> resolves a card's ability before the
    /// card leaves play, and a defeat happens inside <c>Damage.Deal</c>. Keeping
    /// the capabilities on the board lets that nested procedure use the damage
    /// port without threading unrelated card behavior through every caller.
    /// </para>
    /// <para>
    /// Defaults to <see cref="Play.NoCardAbilities"/>, so a board built by hand
    /// is a board where no card does anything — which is what a board built by
    /// hand is.
    /// </para>
    /// </remarks>
    public Play.ICardAbilities Abilities { get; set; } = new Play.NoCardAbilities();

    // The aggregate is retained for host compatibility. Rules code reaches card
    // text through the smallest port that expresses the operation it performs.
    /// <summary>The damage-specific card capability.</summary>
    public Play.ICardDamageAbilities DamageAbilities => (Play.ICardDamageAbilities)Abilities;
    /// <summary>The threat-specific card capability.</summary>
    public Play.IThreatCardAbilities ThreatAbilities => (Play.IThreatCardAbilities)Abilities;
    /// <summary>The power-specific card capability.</summary>
    public Play.ICardPowerAbilities PowerAbilities => (Play.ICardPowerAbilities)Abilities;
    /// <summary>The resource-specific card capability.</summary>
    public Play.IResourceCardAbilities ResourceAbilities => (Play.IResourceCardAbilities)Abilities;
    /// <summary>The encounter-specific card capability.</summary>
    public Play.IEncounterCardAbilities EncounterAbilities => (Play.IEncounterCardAbilities)Abilities;
    /// <summary>The continuation-specific card capability.</summary>
    public Play.ICardContinuationAbilities ContinuationAbilities => (Play.ICardContinuationAbilities)Abilities;
    /// <summary>The activation-completion card capability.</summary>
    public Play.IActivationCompletionAbilities ActivationCompletionAbilities =>
        (Play.IActivationCompletionAbilities)Abilities;
    /// <summary>The readiness-specific card capability.</summary>
    public Play.ICardReadinessAbilities ReadinessAbilities => (Play.ICardReadinessAbilities)Abilities;
    /// <summary>The setup-specific card capability.</summary>
    public Play.ICardSetupAbilities SetupAbilities => (Play.ICardSetupAbilities)Abilities;
    /// <summary>The placement-specific card capability.</summary>
    public Play.ICardPlacementAbilities PlacementAbilities => (Play.ICardPlacementAbilities)Abilities;
    /// <summary>The constant-effect card capability.</summary>
    public Play.ICardConstantAbilities ConstantAbilities => (Play.ICardConstantAbilities)Abilities;
    /// <summary>The action-specific card capability.</summary>
    public Play.ICardActionAbilities ActionAbilities => (Play.ICardActionAbilities)Abilities;
    /// <summary>The timing-window card capability.</summary>
    public Timing.IWindowAbilities WindowAbilities => Abilities;
    /// <summary>The enemy-attack card capability.</summary>
    public Play.IAttackCardAbilities AttackAbilities => (Play.IAttackCardAbilities)Abilities;
    /// <summary>The enter-play card capability.</summary>
    public Play.ICardPlayAbilities CardPlayAbilities => (Play.ICardPlayAbilities)Abilities;
    /// <summary>The encounter-reveal card capability.</summary>
    public Play.IRevealCardAbilities RevealAbilities => (Play.IRevealCardAbilities)Abilities;
    /// <summary>The counter-pool card capability.</summary>
    public Play.ICardCounterPools CounterPools => Abilities;

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

    /// <inheritdoc cref="WorldTopology.Shuffle"/>
    public bool Shuffle(Area area) => WorldTopology.Shuffle(this, area);
    /// <inheritdoc cref="WorldTopology.CreateGameArea"/>
    public GameArea CreateGameArea() => WorldTopology.CreateGameArea(this);
    /// <inheritdoc cref="WorldTopology.Join"/>
    public void Join(
        PlayArea area, GameArea destination, string trigger, List<GameEvent> events) =>
        WorldTopology.Join(this, area, destination, trigger, events);
    /// <inheritdoc cref="WorldTopology.Detach"/>
    public void Detach(PlayArea area, string trigger, List<GameEvent> events) =>
        WorldTopology.Detach(this, area, trigger, events);
    /// <inheritdoc cref="WorldTopology.GameAreaOf"/>
    public GameArea? GameAreaOf(PlayArea area) => WorldTopology.GameAreaOf(this, area);
    /// <inheritdoc cref="WorldTopology.CreateSeat"/>
    public Seat CreateSeat(string name) => WorldTopology.CreateSeat(this, name);
    /// <inheritdoc cref="WorldTopology.AreaOf"/>
    public Area AreaOf(
        DeckType type, PlayArea? playArea = null, int host = -1, int cardOwner = Scenario) =>
        WorldTopology.AreaOf(this, type, playArea, host, cardOwner);
    /// <inheritdoc cref="WorldTopology.TheCardIn"/>
    public Card? TheCardIn(DeckType type) => WorldTopology.TheCardIn(this, type);
    /// <inheritdoc cref="WorldTopology.CreateArea"/>
    public Area CreateArea(
        DeckType type, int cardOwner = -1, PlayArea? playArea = null, int host = -1) =>
        WorldTopology.CreateArea(this, type, cardOwner, playArea, host);
    /// <inheritdoc cref="WorldTopology.CreateCard"/>
    public Card CreateCard(string spec, Area into) =>
        WorldTopology.CreateCard(this, spec, into);

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
                if (_seats.Count > seat && !_seats[seat].Eliminated)
                {
                    yield return seat;
                }
            }
        }
    }

    /// <summary>Moves a card to the end (the top) of an area.</summary>
    /// <param name="card">The card.</param>
    /// <param name="destination">Where it goes.</param>
    public static void MoveToTop(Card card, Area destination)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(destination);
        destination.ValidateCanAcceptCards();

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
        foreach (var area in _areas)
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

        var records = new List<CardRecord>(_cards.Count);
        foreach (var card in _cards)
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
