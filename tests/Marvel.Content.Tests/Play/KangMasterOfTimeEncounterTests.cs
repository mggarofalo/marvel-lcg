using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Master of Time encounter faces and the full authored book on a
// nonpublic Rhino board; Kang's public admission stays closed.
public sealed class KangMasterOfTimeEncounterTests : CoreCardTestBase
{
    private const string MasterOfTime = "11047";

    // Hard to Keep Down and the Temporal attachment Frozen in Time, neither a minion.
    private static readonly string[] NoMinion = ["01104", "11016"];

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:per-player-icon")]
    [Rule("rr:hazard-icon")]
    public void LightOfCenturiesSphereEntersWithPerPlayerThreatAndAHazard(int players)
    {
        // 11050 prints "Starting Threat 3[per_hero]" and Hazard 1.
        // rr:per-player-icon: "multiplies that value by the number of players who
        // started the scenario."
        var (world, f) = KangTemporalFixture.Board(players);
        var sphere = f.Reveal("11050");

        f.Finish();

        Assert.Equal(DeckType.SideSchemesArea, sphere.Area.Type);
        Assert.Equal(3 * players, sphere.Tokens["k_threat"]);
        Assert.Equal(1, Deal.HazardIcons(world, Cards));
    }

    [Fact]
    [Rule("rr:when-defeated-abilities.2")]
    [Rule("rr:when-revealed-abilities.2")]
    public void DefeatedSphereEngagesTheFirstDiscardedMinionWithTheDefeatingPlayer()
    {
        // 11050: "When Defeated: Discard cards from the top of the encounter deck
        // until a minion is discarded. Put that minion into play engaged with the
        // player who defeated this scheme." Player 1, not the first player,
        // defeats it. The Temporal attachment Frozen in Time does not stop the
        // discard. rr:when-revealed-abilities.2: Wildrun is put into play, not
        // revealed, so its When Revealed discards nothing from player 1's hand.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var below = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var wildrun = world.CreateCard("11044", world.AreaOf(DeckType.EncounterDeck));
        var frozen = world.CreateCard("11016", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01088", world.Seats[1].Hand);
        var sphere = Sphere(world);

        Thwart(world, f, sphere, 1);

        Assert.Equal([wildrun], world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Equal(new[] { frozen.ObjectId, sphere.ObjectId }.Order(),
            world.AreaOf(DeckType.EncounterDiscardPile).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([below], world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    public void DefeatedSphereWithoutAMinionStopsWhenTheEncounterDeckEmpties()
    {
        // rr:encounter-deck.2: "If the encounter deck is emptied this way, that card
        // ability is considered to be fulfilled. Do not continue the discard effect
        // with the newly shuffled encounter deck." rr:encounter-deck.1 shuffles the
        // discard pile in and places an acceleration token. The Wildrun already
        // in the discard pile is shuffled in, not put into play.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var old = world.CreateCard("11044", world.AreaOf(DeckType.EncounterDiscardPile));
        var deck = NoMinion
            .Select(face => world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck))).ToArray();
        var sphere = Sphere(world);

        Thwart(world, f, sphere, 1);

        Assert.Equal(deck.Append(old).Select(card => card.ObjectId).Order(),
            world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal([sphere], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:activation.1")]
    [Rule("rr:activation.4")]
    [Rule("rr:villainous")]
    public void AncientGrudgeActivatesMasterOfTimeAgainstTheRevealingPlayer(bool alterEgo)
    {
        // 11051: "Kang (Master of Time) activates against you." He is engaged with
        // player 0; player 1 reveals. rr:activation.1 reads player 1's form: an
        // attack against a hero, a scheme against an alter-ego. rr:activation.4:
        // these "are also considered activations", so rr:villainous gives him a
        // boost card (Hard to Keep Down, no icon). Ruling 13454feafb65378b: his
        // "your play area" is that of the player he activates against, so player
        // 1's one obligation counts and player 0's two do not.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 4);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 0);
        KangTemporalFixture.Obligations(world, 0, 2);
        KangTemporalFixture.Obligations(world, 1, 1);
        if (alterEgo) world.Seats[1].IdentityCard.TurnTo("01001b");
        var grudge = f.Reveal("11051", player: 1);

        var events = f.Finish();

        Assert.Equal(alterEgo ? 0 : 1 + 1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(alterEgo ? 1 + 1 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(alterEgo ? 0 : 1, events.OfType<AttackCompleted>().Count(e => e.Enemy == kang.ObjectId));
        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)), kang.Area);
        Assert.Equal(3, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, grudge.Area.Type);
        Assert.Equal(3 + 3, MasterOfTimeStat(world, kang));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(DeckType.EncounterDeck)]
    [InlineData(DeckType.EncounterDiscardPile)]
    [Rule("rr:search.3")]
    [Rule("rr:when-revealed-abilities.2")]
    [Rule("rr:toughness.1")]
    public void AncientGrudgeFetchesAnAbsentMasterOfTimeEngagedWithTheRevealingPlayer(DeckType from)
    {
        // 11051: "If Kang (Master of Time) is not in play, search the encounter deck
        // and discard pile for Kang (Master of Time) and put him into play engaged
        // with you. Shuffle the encounter deck." He was not in play to activate, and
        // entering does not activate him. rr:toughness.1 still gives him a tough
        // card. rr:search.3: "shuffle that entire deck."
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var deck = Distinct(world);
        var kang = world.CreateCard(MasterOfTime, world.AreaOf(from));
        var before = Order(world);
        f.Reveal("11051", player: 1);

        var events = f.Finish();

        Assert.Equal([kang], world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Single(Statuses.On(world, kang, Statuses.Tough));
        Assert.Empty(events.OfType<AttackCompleted>());
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(deck.Select(card => card.ObjectId).Order(), Order(world).Order());
        Assert.NotEqual(before.Where(id => id != kang.ObjectId), Order(world));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:search.3")]
    public void AncientGrudgeWithoutMasterOfTimeAnywhereStillShufflesTheEncounterDeck()
    {
        // Nothing activates and the search finds nothing; rr:search.3 still
        // shuffles the searched deck, and the printed shuffle is its own sentence.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var deck = Distinct(world);
        var before = Order(world);
        var grudge = f.Reveal("11051", player: 1);

        var events = f.Finish();

        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Empty(events.OfType<AttackCompleted>());
        Assert.Equal(deck.Select(card => card.ObjectId).Order(), Order(world).Order());
        Assert.NotEqual(before, Order(world));
        Assert.Equal([grudge], world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void AncientGrudgeWithMasterOfTimeInPlayShufflesTheEncounterDeckAfterHisActivation()
    {
        // The printed "Shuffle the encounter deck." is a separate sentence and is
        // not governed by "If Kang (Master of Time) is not in play"; the deck is
        // shuffled after his activation draws its boost card.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        var deck = Distinct(world);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 1);
        var boost = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var before = Order(world).Where(id => id != boost.ObjectId).ToList();
        f.Reveal("11051", player: 1);

        var events = f.Finish();

        Assert.Single(events.OfType<AttackCompleted>(), e => e.Enemy == kang.ObjectId);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(deck.Select(card => card.ObjectId).Order(), Order(world).Order());
        Assert.NotEqual(before, Order(world));
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card Sphere(World world)
    {
        var sphere = world.CreateCard("11050", world.AreaOf(DeckType.SideSchemesArea));
        sphere.PlaceTokens("k_threat", 1);
        return sphere;
    }

    private static void Thwart(World world, RedSkullFixture f, Card scheme, int player)
    {
        BasicThwartPowers.BasicThwart(world, Cards, player, scheme, []);
        f.Finish();
    }

    // Eight distinct no-boost encounter cards, so a shuffle is visible in their order.
    private static Card[] Distinct(World world) => [.. Enumerable.Range(0, 8)
        .Select(_ => world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck)))];

    private static List<int> Order(World world) =>
        [.. world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId)];

    private static long MasterOfTimeStat(World world, Card kang) =>
        Modified(world, kang, "attack") + Modified(world, kang, "scheme");
}
