using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// Dealing encounter cards, and the queue that reveals them.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:deal-deal-an-encounter-card</c> separates dealing from revealing by an
/// arbitrary stretch of game: "this card is <b>not revealed at this time</b>.
/// This card is added to the <b>queue</b> of cards that player resolves during
/// the villain phase." Step 4 drains that queue "until no dealt encounter cards
/// remain", which is a loop rather than a list — and the difference is what
/// makes a card dealt by an ability, or by a player's deck running out, get
/// revealed at all.
/// </para>
/// <para>
/// None of this is reachable by the recording: one player, one card, no hazard
/// icons anywhere on the board.
/// </para>
/// </remarks>
public sealed class DealTests
{
    [Rule("rr:villain-phase.step.3")]
    [Fact]
    public void EachPlayerIsDealtOneCardInPlayerOrder()
    {
        var printed = new Printed();
        var world = Board(printed, players: 2);
        world.FirstPlayer = 1;

        var events = Run(world, printed);

        // One each, and the first player's is revealed first. Asserted by seat
        // rather than by card id: every activation takes a boost card off the
        // same deck first, so which face lands where is incidental and the
        // order is the rule.
        Assert.Equal([1, 0], RevealedBy(world, events));

        // **And dealt in that order too**, which is a separate claim. Step 4
        // reveals in player order whatever step 3 put in the queues, so a step
        // 3 that dealt round the wrong way would still be revealed 1 then 0 --
        // and the first player would have had the second card off the deck.
        Assert.Equal([1, 0], DealOrder(events));
    }

    [Rule("rr:hazard-icon")]
    [Rule("rr:hazard-icon.1")]
    [Theory]
    // No icons: one card each and no more.
    [InlineData(0, 2)]
    // "For each hazard icon on cards in play, deal one player one additional
    // card *(not one card per player)*." One icon is one card, not two.
    [InlineData(1, 3)]
    [InlineData(3, 5)]
    public void HazardIconsDealOneExtraCardEach(int icons, int expected)
    {
        var printed = new Printed().With("hazardous", ("Hazard", icons.ToString()));
        var world = Board(printed, players: 2);
        var scheme = world.AreaOf(DeckType.SideSchemesArea);
        world.CreateCard("hazardous", scheme);

        var events = Run(world, printed);

        Assert.Equal(expected, Revealed(world, events).Count);
    }

    [Rule("rr:hazard-icon")]
    [Fact]
    public void ExtraCardsGoRoundTheTableRatherThanToOnePlayer()
    {
        // "Additional cards are dealt in player order *(first additional card
        // to the first player, the second to the second player, etc.)*." Three
        // icons at two players is two extra for the first player and one for
        // the second, not three for anybody.
        var printed = new Printed().With("hazardous", ("Hazard", "3"));
        var world = Board(printed, players: 2);
        world.CreateCard("hazardous", world.AreaOf(DeckType.SideSchemesArea));

        var events = Run(world, printed);

        // One each, then three extra round the table: two to the first player
        // and one to the second.
        Assert.Equal([3, 2], DealtTo(world, events, players: 2));
    }

    [Rule("rr:hazard-icon")]
    [Fact]
    public void OnlyIconsOnCardsInPlayCount()
    {
        // "For each hazard icon on cards **in play**". The encounter deck is
        // full of hazard cards in an ordinary game, and counting those would
        // deal a fistful of cards every round.
        var printed = new Printed().With("hazardous", ("Hazard", "5"));
        var world = Board(printed, players: 1);
        world.CreateCard("hazardous", world.AreaOf(DeckType.EncounterDeck));

        Assert.Equal(0, Deal.HazardIcons(world, printed));
    }

    [Rule("rr:deal-deal-an-encounter-card")]
    [Fact]
    public void ACardDealtOutsideStepThreeIsStillRevealedInStepFour()
    {
        // The whole reason step 4 is a queue. A card dealt at any other moment
        // -- here, before the phase even starts -- joins the queue and is
        // revealed with the rest.
        var printed = new Printed();
        var world = Board(printed, players: 1);
        var events = new List<GameEvent>();

        var early = Deal.EncounterCard(world, 0, "an ability", events)!;
        Run(world, printed, events: events);

        // Two: the one dealt early and the one step 3 dealt. The early one is
        // revealed first, because the queue keeps the order they were dealt in.
        var revealed = Revealed(world, events);
        Assert.Equal(2, revealed.Count);
        Assert.Equal(early.ObjectId, revealed[0]);
    }

    [Rule("rr:deal-deal-an-encounter-card.1")]
    [Fact]
    public void ACardDealtWhileRevealingIsRevealedInTheSameStep()
    {
        // "If a player is dealt an encounter card during step three or four of
        // the villain phase, the extra encounter card is added to the queue of
        // cards that are being dealt and revealed in **those same steps**."
        //
        // This is what a list cannot do and a loop can: the reveal of the first
        // card deals a second, and the second is revealed without waiting for
        // the next round.
        var printed = new Printed();
        var world = Board(printed, players: 1);
        // The first card revealed deals another, and once only -- otherwise
        // this would be a test of whether the deck runs out.
        var events = Run(world, printed, new DealsOnReveal());

        Assert.Equal(2, Revealed(world, events).Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:villain-phase.step.4")]
    [Fact]
    public void TheQueueIsDrainedInPlayerOrderAndThenTheStepEnds()
    {
        var printed = new Printed();
        var world = Board(printed, players: 2);
        var events = new List<GameEvent>();

        Deal.EncounterCard(world, 1, "an ability", events);
        Deal.EncounterCard(world, 0, "an ability", events);

        // The first player's whole queue first, then the next player's.
        Assert.Equal(0, Deal.NextToReveal(world)!.Value.Player);

        Run(world, printed);
        Assert.Null(Deal.NextToReveal(world));
    }

    [Rule("rr:reveal.step.1")]
    [Rule("rr:reveal.step.3")]
    [Rule("rr:reveal.step.4")]
    [Rule("rr:reveal.6")]
    [Rule("rr:reveal.8")]
    [Rule("rr:treachery.1")]
    [Rule("rr:treachery.2")]
    [Fact]
    public void ATreacheryCompletesItsRevealBeforeResponsesResolve()
    {
        // "Turn the encounter card faceup," place a treachery "in front of the
        // player revealing it," and "resolve each 'When Revealed' ability."
        // The revealing player "must resolve its effects" and, "after
        // resolving," place it in the encounter discard pile as step 4 says.
        // Responses "are not resolved until after all steps of the reveal
        // process have been completed," so this one observes the discard.
        var printed = new Printed();
        var world = Board(printed, players: 1);
        var abilities = new ObservingReveal();

        Run(world, printed, abilities);

        Assert.NotNull(abilities.Card);
        Assert.True(abilities.FaceUpDuringWhenRevealed);
        Assert.Equal(DeckType.RevealingArea, abilities.AreaDuringWhenRevealed);
        Assert.Equal(DeckType.EncounterDiscardPile, abilities.Card.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, abilities.AreaDuringResponse);
        Assert.Equal(1, abilities.Responses);
    }

    [Rule("rr:incite-x.1")]
    [Fact]
    public void InciteResolvesDuringRevealAfterTheCardEntersPlay()
    {
        // Incite is equivalent to "When Revealed: Place X threat on the main
        // scheme." Its threat event therefore follows both step 1 turning the
        // card faceup and step 2 putting this side scheme into play.
        var printed = new Printed().With("incite", ("Incite", "2"));
        var world = Board(printed, players: 1);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var incite = world.CreateCard("incite", deck);
        var events = new List<GameEvent>();
        Deal.EncounterCard(world, 0, "test", events);

        Run(world, printed, events: events);

        int flipped = events.FindIndex(item => item is CardsFlipped value
            && value.Verb == "Reveal"
            && value.Cards.Contains(incite.ObjectId));
        int entered = events.FindIndex(item => item is CardsMoved value
            && value.To.Zone == nameof(DeckType.SideSchemesArea)
            && value.Cards.Any(landing => landing.Card == incite.ObjectId));
        int placed = events.FindIndex(item => item is FieldSet value
            && value.Trigger == "incite"
            && value.Card == world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId);

        Assert.True(flipped >= 0);
        Assert.True(entered > flipped);
        Assert.True(placed > entered);
    }

    /// <summary>
    /// The faces revealed, in order.
    /// </summary>
    /// <remarks>
    /// Read off the reveal events and <b>not</b> the encounter discard pile,
    /// which is not the same list: every activation puts a boost card through
    /// that pile too, so a board with two players and no hazard icons
    /// accumulates four cards there and reveals two.
    /// </remarks>
    private static IReadOnlyList<int> Revealed(World world, IEnumerable<GameEvent> events)
    {
        _ = world;
        return [.. events.OfType<CardsFlipped>()
            .Where(flip => flip.Verb == "Reveal")
            .SelectMany(flip => flip.Cards)];
    }

    /// <summary>Which seat each revealed card had been dealt to, in reveal order.</summary>
    private static int[] RevealedBy(World world, IReadOnlyList<GameEvent> events)
    {
        var dealtTo = new Dictionary<int, int>();
        foreach (var moved in events.OfType<CardsMoved>().Where(move => move.Verb == "Deal"))
        {
            foreach (var landing in moved.Cards)
            {
                dealtTo[landing.Card] = moved.To.Owner;
            }
        }

        return [.. Revealed(world, events).Select(id => dealtTo[id])];
    }

    /// <summary>The seats dealt to, in the order the cards were dealt.</summary>
    private static int[] DealOrder(IEnumerable<GameEvent> events) =>
        [.. events.OfType<CardsMoved>()
            .Where(move => move.Verb == "Deal")
            .SelectMany(move => move.Cards.Select(_ => move.To.Owner))];

    /// <summary>How many cards each seat was dealt, by the deal events.</summary>
    private static int[] DealtTo(
        World world, IEnumerable<GameEvent> events, int players)
    {
        _ = world;
        var counts = new int[players];
        foreach (var moved in events.OfType<CardsMoved>().Where(move => move.Verb == "Deal"))
        {
            // The deal event and not where the card is now: by the time the
            // phase ends every one of them has been revealed and discarded to
            // the villain's play area, which is not a seat.
            counts[moved.To.Owner] += moved.Cards.Count;
        }

        return counts;
    }

    private static List<GameEvent> Run(
        World world, Printed printed, ICardAbilities? abilities = null,
        List<GameEvent>? events = null)
    {
        events ??= [];
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, abilities ?? new Silent(), events);
        return events;
    }

    /// <summary>A villain, a scheme, one identity per seat, six encounter cards.</summary>
    private static World Board(Printed printed, int players)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard("identity", world.Seats[seat].Hero);
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));

        // The deck is drawn from the top, which is the end of the list, so `e5`
        // is dealt first.
        // Deep enough that no test here is really testing what happens when
        // the encounter deck runs out: every activation takes a boost card off
        // it before step 3 is reached.
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int card = 0; card < 16; card++)
        {
            world.CreateCard($"e{card}", deck);
        }

        return world;
    }

    /// <summary>Nothing waits in a window, and nothing is revealed.</summary>
    private sealed class Silent : NoCardAbilities
    {


        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => [];

    }

    /// <summary>One card whose "When Revealed" deals another encounter card.</summary>
    private sealed class DealsOnReveal : NoCardAbilities
    {
        private bool used;

        public override IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
        {
            if (used)
            {
                return [];
            }

            used = true;
            var events = new List<GameEvent>();
            Deal.EncounterCard(world, player, "when revealed", events);
            return events;
        }


        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => [];

    }

    /// <summary>Observes the treachery during its own text and after all reveal steps.</summary>
    private sealed class ObservingReveal : NoCardAbilities
    {
        public Card? Card { get; private set; }

        public DeckType AreaDuringWhenRevealed { get; private set; }

        public bool FaceUpDuringWhenRevealed { get; private set; }

        public DeckType AreaDuringResponse { get; private set; }

        public int Responses { get; private set; }

        public override IReadOnlyList<GameEvent> WhenRevealed(
            World world, Card card, int player)
        {
            Card = card;
            AreaDuringWhenRevealed = card.Area.Type;
            FaceUpDuringWhenRevealed = card.FaceUp;
            return [];
        }

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == WindowKind.Response
            && occurrence.Conditions.Contains(Steps.CardRevealed, StringComparer.Ordinal)
                ? [new PendingAbility(occurrence.Subject, AbilityType.ForcedResponse, 0)]
                : [];

        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
        {
            Responses += 1;
            AreaDuringResponse = world.Cards[ability.Card].Area.Type;
            return [];
        }
    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "incite" => CardKind.EncounterSideScheme,
            "hazardous" => CardKind.EncounterSideScheme,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? printed)
            && long.TryParse(printed, out long value)
                ? value
                : fallback;
    }
}
