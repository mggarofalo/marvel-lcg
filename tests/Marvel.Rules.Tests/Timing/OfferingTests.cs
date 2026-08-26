using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

/// <summary>
/// Who gets asked, who gets told, and who is passed over in silence.
/// </summary>
public sealed class OfferingTests
{
    [Rule("rr:interrupt.5")]
    [Fact]
    public void AWindowWithNothingInItAsksNobodyAnything()
    {
        // Every occurrence in the game opens two windows and almost none has an
        // eligible ability in it. An engine that asked "any interrupts?" before
        // every threat token would be asking a question with one possible
        // answer.
        var world = Board(players: 3);
        var events = new List<GameEvent>();

        var prompt = Offering.Work(
            world, new Cards(), Moment(), WindowKind.Interrupt, events);

        Assert.Null(prompt);
        Assert.Empty(events);
        Assert.False(world.Windows.IsResolving);
    }

    [Rule("rr:ability.11")]
    [Fact]
    public void APlayerWithNothingEligibleIsSkippedRatherThanAsked()
    {
        // Being skipped is not declining. p0 and p2 are never asked, because
        // there is nothing they could have said.
        var world = Board(players: 3);
        var cards = new Cards(new PendingAbility(5, AbilityType.Interrupt, 1));

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
    }

    [Rule("rr:ability.11")]
    [Fact]
    public void OneOptionalAbilityIsStillAChoiceBecauseDecliningIsTheOther()
    {
        // "Unless prefaced by the word 'Forced', all interrupt and response
        // abilities are optional." So an offer of exactly one ability is a real
        // question, and the prompt says so by being cancellable.
        var world = Board(players: 1);
        var cards = new Cards(new PendingAbility(5, AbilityType.Interrupt, 0));

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.True(prompt.Cancellable);
        Assert.Equal(Question.Opportunity, prompt.Asking);
        Assert.Equal(TimingPriority.Interrupt, prompt.When);
        Assert.Single(prompt.Affordances);
    }

    [Rule("rr:forced.1")]
    [Fact]
    public void AForcedAbilityIsResolvedAndReportedRatherThanOffered()
    {
        // "'Forced Interrupt' and 'Forced Response' abilities must be resolved
        // when their triggering conditions are met." There is no choice to put
        // to anybody, so what reaches the client is an event: this happened.
        var world = Board(players: 2);
        var cards = new Cards(new PendingAbility(5, AbilityType.ForcedInterrupt, 0));
        var events = new List<GameEvent>();

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, events);

        Assert.Null(prompt);
        Assert.Single(events);
        Assert.Equal([5], cards.Resolved);
    }

    [Rule("rr:forced.4")]
    [Fact]
    public void AForcedAbilityIsResolvedBeforeAnOptionalOneIsOffered()
    {
        // "Forced interrupts take priority and initiate before non-forced
        // interrupts." So the optional one is not offered until the forced one
        // has resolved -- and by then the board may be different.
        var world = Board(players: 1);
        var cards = new Cards(
            new PendingAbility(5, AbilityType.Interrupt, 0),
            new PendingAbility(6, AbilityType.ForcedInterrupt, 0));
        var events = new List<GameEvent>();

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, events);

        Assert.Equal([6], cards.Resolved);
        Assert.NotNull(prompt);
        Assert.Equal(TimingPriority.Interrupt, prompt.When);
    }

    [Rule("rr:forced.5")]
    [Fact]
    public void TwoForcedAbilitiesAtOnceAskTheFirstPlayerForTheOrder()
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the first player determines the order in which the abilities
        // initiate, regardless of who controls the cards." It is a real
        // question, and the only one here: they all resolve either way, so
        // declining is not an answer.
        var world = Board(players: 3);
        world.FirstPlayer = 2;
        var cards = new Cards(
            new PendingAbility(5, AbilityType.ForcedInterrupt, 0),
            new PendingAbility(6, AbilityType.ForcedInterrupt, 1));

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.Equal(Question.Order, prompt.Asking);
        Assert.Equal(2, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(2, prompt.Affordances.Count);
        Assert.Empty(cards.Resolved);
    }

    [Rule("rr:interrupt.1")]
    [Rule("rr:ability.8")]
    [Fact]
    public void AnAbilityOnAnEncounterCardIsOfferedToWhoeverIsAsked()
    {
        // "Players can only trigger interrupt abilities on cards they control
        // or on encounter cards", and any player may use the latter. So an
        // ability with no controller reaches the first player asked rather than
        // waiting for an owner who does not exist.
        var world = Board(players: 3);
        world.FirstPlayer = 1;
        var cards = new Cards(new PendingAbility(5, AbilityType.Interrupt, -1));

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
    }

    [Rule("rr:forced.6")]
    [Fact]
    public void EachForcedAbilityResolvesCompletelyBeforeTheNextInitiates()
    {
        // "Each forced ability must resolve as completely as possible before
        // the next forced ability being triggered by the same triggering
        // condition may initiate." So the board is re-read between them: a card
        // whose ability removes another card's is obeyed.
        var world = Board(players: 1);
        // Different tiers, so they resolve one at a time rather than becoming
        // an ordering question: a status card's forced interrupt is 2a and an
        // ordinary one is 2b (`rr:ability.step.2.a`).
        var cards = new Cards(new PendingAbility(5, AbilityType.StatusForcedInterrupt, 0))
        {
            Withdraw = 6,
        };
        cards.Add(new PendingAbility(6, AbilityType.ForcedInterrupt, 0));

        Offering.Work(world, cards, Moment(), WindowKind.Interrupt, []);

        Assert.Equal([5], cards.Resolved);
    }

    

    [Rule("rr:peril")]
    [Fact]
    public void OnlyTheResolvingPlayerActsWhileAPerilCardIsResolving()
    {
        // "While a player is resolving this card, that player cannot consult
        // other players, and **other players cannot trigger abilities**." Not
        // "abilities on this card" -- *any* ability. A peril card is resolved
        // alone.
        //
        // The window here holds one ability apiece for two of the three
        // players. Without the keyword the first player is asked first; with
        // it, the player resolving the card is the only one asked at all.
        var world = Board(players: 3);
        var card = world.CreateCard("peril", world.AreaOf(DeckType.RevealingArea));
        var resolving = new Occurrence(
            1, ["WhenCardRevealed"], Subject: card.ObjectId, Player: 2);
        var cards = new Cards(
            new PendingAbility(5, AbilityType.Interrupt, 0),
            new PendingAbility(6, AbilityType.Interrupt, 2));

        var prompt = Offering.Work(world, cards, resolving, WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.Equal(2, prompt.Player);
        Assert.Single(prompt.Affordances);
    }

    [Rule("rr:peril")]
    [Fact]
    public void WithoutPerilTheFirstPlayerIsAskedFirst()
    {
        // The same board and the same two abilities, on a card that is not
        // perilous. `rr:first-player.4` gives the first player the first
        // opportunity, so this is what the keyword is taking away.
        var world = Board(players: 3);
        var card = world.CreateCard("ordinary", world.AreaOf(DeckType.RevealingArea));
        var resolving = new Occurrence(
            1, ["WhenCardRevealed"], Subject: card.ObjectId, Player: 2);
        var cards = new Cards(
            new PendingAbility(5, AbilityType.Interrupt, 0),
            new PendingAbility(6, AbilityType.Interrupt, 2));

        var prompt = Offering.Work(world, cards, resolving, WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.Equal(0, prompt.Player);
    }

    [Rule("rr:peril.1")]
    [Fact]
    public void AbilitiesOnAPerilCardInYourAreaAreYoursAlone()
    {
        // The second clause, and a different rule from the first: "while this
        // card is **in a player's play area**, other players cannot trigger
        // abilities on this card." It is narrower -- only abilities on that
        // card -- and it lasts as long as the card sits there rather than only
        // while it resolves.
        //
        // The ability below has no controller, which `rr:ability.8` would
        // otherwise let any player use.
        var world = Board(players: 2);
        var card = world.CreateCard(
            "peril", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        var cards = new Cards(new PendingAbility(card.ObjectId, AbilityType.Interrupt, -1));

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
    }

    [Rule("rr:ability.8")]
    [Fact]
    public void AnEncounterCardWithoutPerilIsAnybodysToTrigger()
    {
        // The converse, and the reason the clause above is a restriction rather
        // than how the engine already worked: "players can only trigger
        // interrupt abilities on cards they control **or on encounter cards**",
        // and any player may use the latter.
        var world = Board(players: 2);
        var card = world.CreateCard(
            "ordinary", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        var cards = new Cards(new PendingAbility(card.ObjectId, AbilityType.Interrupt, -1));

        var prompt = Offering.Work(world, cards, Moment(), WindowKind.Interrupt, []);

        Assert.NotNull(prompt);
        Assert.Equal(0, prompt.Player);
    }

    private static Occurrence Moment() => new(1, "WhenAttacked");

    private static World Board(int players)
    {
        var world = new World(new Facts(), players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
        }

        return world;
    }

    /// <summary>A fixed set of waiting abilities, and a record of what resolved.</summary>
    private sealed class Cards(params PendingAbility[] abilities) : IWindowAbilities
    {
        private readonly List<PendingAbility> waiting = [.. abilities];

        public List<int> Resolved { get; } = [];

        /// <summary>A card this removes from the window once anything resolves.</summary>
        public int? Withdraw { get; set; }

        public void Add(PendingAbility ability) => waiting.Add(ability);

        public IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            [.. waiting.Where(ability => ability.Card != WithdrawnCard)];

        public IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying)
        {
            Resolved.Add(ability.Card);
            return [new CardsFlipped([ability.Card], true) { Trigger = "test" }];
        }

        public Affordance Describe(World world, PendingAbility ability) =>
            new(ability.Card, "Use", ability.Card, ability.Player, $"ability {ability.Card}");

        private int? WithdrawnCard => Resolved.Count > 0 ? Withdraw : null;
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            string.Equals(faceId, "peril", StringComparison.Ordinal)
            && string.Equals(attribute, "Peril", StringComparison.Ordinal)
                ? 1
                : fallback;
    }
}
