using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.GalaxyModularBoards;

namespace Marvel.Content.Tests.Play;

// The production Space Pirates modular set on synthetic boards.
public sealed class SpacePiratesTests : CoreCardTestBase
{
    private const string HydraMercenary = "01101";
    private const string BlackjackOHare = "16055";

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [Rule("rr:damage")]
    public void PirateCommanderRemovesARandomHandCardAfterItsAttackDamagesYou(bool tough, int removed)
    {
        // 16138: "Forced Response: After this minion attacks and damages you, remove 1 card at
        // random in your hand from the game." A tough status card leaves no damage.
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        if (tough) Statuses.Give(world, hero, Statuses.Tough);
        var hand = new[] { GrootBoards.Hand(world, "01003"), GrootBoards.Hand(world, "01004") };
        var commander = GrootBoards.Engaged(world, PirateCommander);

        KangTemporalFixture.Attack(world, commander, 0);
        Drive(world);

        Assert.Equal(removed, hand.Count(card => card.Area.Type == DeckType.RemovedArea));
        Assert.Equal(2 - removed, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(tough ? 0 : 2, hero.Damage);
    }

    [Fact]
    [Rule("rr:player-deck.1")]
    public void PirateLackeyRemovesTheTopCardOfYourDeck()
    {
        // 16139: "Forced Response: After this minion attacks and damages you, remove the top card
        // of your deck from the game."
        var world = Board();
        var deck = world.Seats[0].Deck;
        var top = deck.Cards[^1];
        var lackey = GrootBoards.Engaged(world, PirateLackey);

        KangTemporalFixture.Attack(world, lackey, 0);
        Drive(world);

        Assert.Equal(DeckType.RemovedArea, top.Area.Type);
        Assert.Equal(11, deck.Cards.Count);
    }

    [Fact]
    [Rule("rr:player-deck.1")]
    public void ADeckThatPirateLackeyEmptiesResetsAndDealsAnEncounterCard()
    {
        // "If a player deck empties, the player shuffles their discard pile to make a new deck.
        // That player immediately deals themself one facedown encounter card."
        var world = Board();
        var deck = world.Seats[0].Deck;
        var pile = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        while (deck.Cards.Count > 1) World.MoveToTop(deck.Cards[0], pile);
        var last = deck.Cards[0];
        var lackey = GrootBoards.Engaged(world, PirateLackey);

        KangTemporalFixture.Attack(world, lackey, 0);
        Drive(world);

        Assert.Equal(DeckType.RemovedArea, last.Area.Type);
        Assert.Equal(11, deck.Cards.Count);
        Assert.Empty(pile.Cards);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData(PirateLackey, 4)]
    [InlineData(HydraMercenary, 3)]
    [Rule("rr:boost-boost-icon.4")]
    public void APirateGivesTheActivatingVillainAnAdditionalBoostCard(string boost, long damage)
    {
        // "Boost: Give the villain 1 additional boost card for this activation." Beneath the pirate
        // is Hydra Mercenary (one icon): Rhino's 2, the pirate's 1 and the Mercenary's 1.
        var world = Board();
        var mercenary = OnTop(world, HydraMercenary);
        OnTop(world, boost);

        VillainAttacks(world);

        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(boost == PirateLackey, mercenary.Area.Type == DeckType.EncounterDiscardPile);
    }

    [Fact]
    [Rule("rr:villainous")]
    public void APirateGivesTheVillainNothingWhenAVillainousMinionActivates()
    {
        // Ruling fba0af85176f8559: "since the villain is not activating, do not give it a boost
        // card." Blackjack O'Hare (villainous, ATK 1) attacks with Pirate Lackey's one icon.
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var mercenary = OnTop(world, HydraMercenary);
        OnTop(world, PirateLackey);
        var blackjack = GrootBoards.Engaged(world, BlackjackOHare);

        KangTemporalFixture.Attack(world, blackjack, 0);
        Drive(world);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDeck, mercenary.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.BoostCardsDeck, villain.Area.PlayArea, villain.ObjectId).Cards);
    }

    [Fact]
    [Rule("rr:constant-ability")]
    public void SoundTheAlarmsGivesEachEnemyOneAttackWhileItIsInPlay()
    {
        // 16140: "Each enemy gets +1 ATK."
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var minion = GrootBoards.Engaged(world, KreePrivate);
        var alarms = GrootBoards.Side(world, SoundTheAlarms, 2);

        Assert.Equal(3, Modified(world, villain, "attack"));
        Assert.Equal(2, Modified(world, minion, "attack"));

        World.MoveToTop(alarms, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal(2, Modified(world, villain, "attack"));
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    public void SoundTheAlarmsAsABoostCardIsRevealed()
    {
        // "Boost: Reveal this card." It enters play with 2 threat per player and raises the
        // attack it boosted, whose ATK was already fixed.
        var world = Board(players: 2);
        var alarms = OnTop(world, SoundTheAlarms);

        VillainAttacks(world);

        Assert.Equal(DeckType.SideSchemesArea, alarms.Area.Type);
        Assert.Equal(4, alarms.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3, Modified(world, GrootBoards.Villain(world), "attack"));
    }

    [Fact]
    [Rule("rr:quickstrike")]
    public void HonorAmongThievesRevealsACriminalMinionGivesItToughAndBoostsTheVillain()
    {
        // 16141: "Discard cards from the top of the encounter deck until a Criminal minion is
        // discarded. Reveal that minion, then give that minion a tough status card and the
        // villain 1 facedown boost card." Pirate Lackey's quickstrike attack damages the hero,
        // so its response removes the top card of the deck.
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var lackey = OnTop(world, PirateLackey);
        var mercenary = OnTop(world, HydraMercenary);
        var top = world.Seats[0].Deck.Cards[^1];
        ShipCommandBoards.Reveal(world, HonorAmongThieves);

        Drive(world);

        Assert.Equal(DeckType.EncounterDiscardPile, mercenary.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, lackey.Area.Type);
        Assert.True(Statuses.Has(world, lackey, Statuses.Tough));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.RemovedArea, top.Area.Type);
        Assert.Single(world.AreaOf(DeckType.BoostCardsDeck, villain.Area.PlayArea, villain.ObjectId).Cards);
    }
}
