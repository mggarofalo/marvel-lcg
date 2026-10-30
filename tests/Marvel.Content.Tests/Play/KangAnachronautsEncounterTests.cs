using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Anachronauts encounter faces and the full authored book on a nonpublic
// Rhino board; Kang's public admission stays closed.
public sealed class KangAnachronautsEncounterTests : CoreCardTestBase
{
    // Hard to Keep Down, the Temporal attachment Frozen in Time, HYDRA Mercenary.
    private static readonly string[] NoTemporalMinion = ["01104", "11016", "01101"];

    // Ancient Warrior (a Temporal minion) and Frozen in Time (a Temporal attachment).
    private static readonly string[] TemporalCards = ["11030", "11016"];

    // HYDRA Mercenary and Kang's Chosen, neither carrying the Temporal trait.
    private static readonly string[] OtherCards = ["01101", "11046"];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [Rule("rr:you-your.8")]
    public void RevealedWildrunDiscardsOneRandomCardFromTheRevealingPlayersHand(int hand)
    {
        // 11044: "When Revealed: Discard 1 random card from your hand."
        // rr:you-your.8: "the player resolving that game step or card ability
        // discards the cards from their hand." An empty hand discards nothing.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        for (int i = 0; i < 3; i++) world.CreateCard("01088", world.Seats[0].Hand);
        for (int i = 0; i < hand; i++) world.CreateCard("01088", world.Seats[1].Hand);
        var wildrun = f.Reveal("11044", player: 1);

        f.Finish();

        Assert.Equal(DeckType.EngagedEnemiesArea, wildrun.Area.Type);
        Assert.Equal(PlayArea.Of(1), wildrun.Area.PlayArea);
        Assert.Equal(Math.Max(0, hand - 1), world.Seats[1].Hand.Cards.Count);
        Assert.Equal(Math.Min(1, hand), PlayerDiscard(world, 1).Count);
        Assert.Equal(3, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void WildrunsRandomDiscardIsTheSeededStreamsChoice()
    {
        // AGENTS.md non-negotiable 5: the seeded stream is the only randomness. No
        // earlier draw precedes the pick on this board, so the discard is the
        // seed's first choice over the hand.
        var picks = new HashSet<int>();
        for (uint seed = 1; seed <= 8; seed++)
        {
            int expected = new Marvel.Core.Random.EngineRandom(seed).Choice(Enumerable.Range(0, 5).ToArray());
            Assert.Equal(expected, DiscardedPosition(seed));
            Assert.Equal(expected, DiscardedPosition(seed));
            picks.Add(expected);
        }
        Assert.True(picks.Count > 1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:incite-x.1")]
    [Rule("rr:simultaneous-resolution")]
    public void KangsChosenDiscardsPastOtherCardsAndRevealsTheFirstTemporalMinion(bool inciteFirst)
    {
        // 11046: "Incite 1. When Revealed: Discard cards from the top of the
        // encounter deck until a Temporal minion is discarded. Reveal that minion."
        // rr:incite-x.1: "When Revealed: Place X threat on the main scheme." A HYDRA
        // minion and the Temporal attachment Frozen in Time do not stop the discard.
        // Revealed, Wildrun resolves its own When Revealed for the revealing player.
        // rr:simultaneous-resolution: "the first player determines the order", and
        // either order reaches the same board.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var below = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var wildrun = world.CreateCard("11044", world.AreaOf(DeckType.EncounterDeck));
        var frozen = world.CreateCard("11016", world.AreaOf(DeckType.EncounterDeck));
        var mercenary = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        for (int i = 0; i < 2; i++)
        {
            world.CreateCard("01088", world.Seats[0].Hand);
            world.CreateCard("01088", world.Seats[1].Hand);
        }
        var chosen = f.Reveal("11046", player: 1);
        var askers = new List<int>();

        f.Finish(prompt =>
        {
            askers.Add(prompt.Player);
            return Order(prompt, inciteFirst);
        });

        Assert.Equal([0], askers);
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), wildrun.Area);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(new[] { mercenary, frozen, chosen }.Select(card => card.ObjectId).Order(),
            world.AreaOf(DeckType.EncounterDiscardPile).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([below], world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    public void KangsChosenWithoutATemporalMinionStopsWhenTheEncounterDeckEmpties()
    {
        // rr:encounter-deck.2: "discard cards from the encounter deck until the
        // discard condition is met or the encounter deck is empty. If the encounter
        // deck is emptied this way, that card ability is considered to be fulfilled.
        // Do not continue the discard effect with the newly shuffled encounter deck."
        // rr:encounter-deck.1 shuffles the discard pile in and places an acceleration
        // token. The Wildrun already discarded is shuffled in, not revealed.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var wildrun = world.CreateCard("11044", world.AreaOf(DeckType.EncounterDiscardPile));
        var deck = NoTemporalMinion
            .Select(face => world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck))).ToArray();
        world.CreateCard("01088", world.Seats[1].Hand);
        var chosen = f.Reveal("11046", player: 1);

        f.Finish(prompt => Order(prompt, inciteFirst: true));

        Assert.Equal(deck.Append(wildrun).Select(card => card.ObjectId).Order(),
            world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([chosen], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:encounter-deck.2")]
    public void KangsChosenRevealsATemporalMinionThatWasTheLastCard()
    {
        // rr:encounter-deck.2 stops "until the discard condition is met or the
        // encounter deck is empty"; the last card meets the condition, so the
        // discarded Wildrun is revealed after the reset rather than shuffled away.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var old = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDiscardPile));
        var wildrun = world.CreateCard("11044", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01088", world.Seats[1].Hand);
        f.Reveal("11046", player: 1);

        f.Finish(prompt => Order(prompt, inciteFirst: true));

        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), wildrun.Area);
        Assert.Equal([old], world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:unique-icon.4")]
    [Rule("rr:unique-icon.4.2")]
    public void KangsChosenRevealingASecondWildrunDiscardsItAndDealsAReplacement()
    {
        // rr:unique-icon.4.2: a matching non-villain encounter card "is discarded and
        // any effects of it entering play are ignored. If it was being revealed, any
        // effects of it being revealed are ignored and the player revealing it is
        // dealt a facedown encounter card." Wildrun prints the unique icon.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var inPlay = KangTemporalFixture.Engaged(world, "11044", 0);
        var copy = world.CreateCard("11044", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01088", world.Seats[1].Hand);
        f.Reveal("11046", player: 1);

        f.Finish(prompt => Order(prompt, inciteFirst: true));

        Assert.Equal(DeckType.EncounterDiscardPile, copy.Area.Type);
        Assert.Equal([inPlay], world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:per-player-icon")]
    [Rule("rr:hazard-icon")]
    [Rule("rr:when-defeated-abilities.2")]
    public void DefeatedAnachronautsShufflesOnlyTemporalCardsFromTheEncounterDiscard(int players)
    {
        // 11045 prints "Starting Threat 3[per_hero]" and Hazard 1. rr:per-player-icon:
        // "multiplies that value by the number of players who started the scenario."
        // rr:when-defeated-abilities.2: "When a ... side scheme ... is defeated, all
        // 'When Defeated' abilities on the card resolve." 11045: "Shuffle each
        // Temporal card in the encounter discard pile into the encounter deck."
        var (world, f) = KangTemporalFixture.Board(players);
        var scheme = f.Reveal("11045");
        f.Finish();
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(3 * players, scheme.Tokens["k_threat"]);
        Assert.Equal(1, Deal.HazardIcons(world, Cards));

        var pile = world.AreaOf(DeckType.EncounterDiscardPile);
        var temporal = TemporalCards.Select(face => world.CreateCard(face, pile)).ToArray();
        var other = OtherCards.Select(face => world.CreateCard(face, pile)).ToArray();
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        scheme.PlaceTokens("k_threat", 2 - (3 * players));
        BasicThwartPowers.BasicThwart(world, Cards, 0, scheme, []);
        var events = f.Finish();

        Assert.All(temporal, card => Assert.Equal(DeckType.EncounterDeck, card.Area.Type));
        Assert.Equal(deck + 2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(other.Append(scheme).Select(card => card.ObjectId).Order(),
            pile.Cards.Select(card => card.ObjectId).Order());
        Assert.Equal(temporal.Select(card => card.ObjectId).Order(), events.OfType<CardsMoved>()
            .Where(e => e.Verb == "Shuffle_Into").SelectMany(e => e.Cards).Select(c => c.Card).Order());
        Assert.Equal(0, Deal.HazardIcons(world, Cards));
        Assert.False(world.Agenda.IsBusy);
    }

    private static Decision Order(Prompt prompt, bool inciteFirst) =>
        Decision.Take(Assert.Single(prompt.Affordances, option => (option.Label == "Incite") == inciteFirst).Id);

    private static List<Card> PlayerDiscard(World world, int player) =>
        [.. world.Cards.Where(card => card.Area.Type == DeckType.DiscardPile && card.Owner == player)];

    private static int DiscardedPosition(uint seed)
    {
        var (world, f) = KangTemporalFixture.Board(players: 2, seed: seed);
        var hand = Enumerable.Range(0, 5).Select(_ => world.CreateCard("01088", world.Seats[1].Hand)).ToArray();
        f.Reveal("11044", player: 1);
        f.Finish();
        return Array.IndexOf(hand, Assert.Single(PlayerDiscard(world, 1)));
    }
}
