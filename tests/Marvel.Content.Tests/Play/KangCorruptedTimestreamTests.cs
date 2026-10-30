using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Corrupted Timestream (11022) and the full authored book on a nonpublic Rhino
// board; Kang's public admission stays closed.
public sealed class KangCorruptedTimestreamTests : CoreCardTestBase
{
    private const string Timestream = "11022";

    [Fact]
    [Rule("rr:cannot")]
    [Rule("rr:obligation.6")]
    public void NoPlayerCanTriggerAnAlterEgoActionOnAnObligationWhileItIsInPlay()
    {
        // 11022: "Players cannot trigger "Alter-Ego Action" abilities on
        // obligations." Each player's payable obligation action is withheld; Aunt
        // May's Alter-Ego Action is not on an obligation and stays available. Both
        // return when the side scheme leaves play.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var memories = Obligation(world, "11019", 0);
        var hijinks = Obligation(world, "11021", 1);
        var may = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        world.Seats[1].IdentityCard.TakeDamage(2);
        for (int player = 0; player < 2; player++) AlterEgo(world, player);
        InHand(world, "01089", 0);
        InHand(world, "01088", 1);
        var timestream = world.CreateCard(Timestream, world.AreaOf(DeckType.SideSchemesArea));

        Assert.False(Offers(f, memories, 0));
        Assert.False(Offers(f, hijinks, 1));
        Assert.True(Offers(f, may, 1));

        Discard.Card(world, timestream, "test", []);

        Assert.True(Offers(f, memories, 0));
        Assert.True(Offers(f, hijinks, 1));
    }

    [Fact]
    [Rule("rr:each-player")]
    [Rule("rr:each-player.1")]
    public void EachPlayerChoosesARandomDiscardOrTwoThreatHere()
    {
        // 11022: "When Revealed: Each player must either discard 1 random card from
        // hand, or place 2 threat here." Player 1 reveals it. Player 0 discards
        // one of two hand cards; player 1 places threat on top of the printed
        // starting threat 2.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        for (int i = 0; i < 2; i++) { InHand(world, "01088", 0); InHand(world, "01088", 1); }
        var askers = new List<int>();

        var timestream = f.Reveal(Timestream, player: 1);
        f.Finish(prompt =>
        {
            askers.Add(prompt.Player);
            Assert.Equal(2, prompt.Affordances.Count);
            return Decision.Take(prompt.Affordances[prompt.Player == 0 ? 0 : 1].Id);
        });

        Assert.Equal([0, 1], askers);
        Assert.Equal(DeckType.SideSchemesArea, timestream.Area.Type);
        Assert.Equal(2 + 2, timestream.Tokens["k_threat"]);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Single(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
        Assert.Equal(2, world.Seats[1].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    [Rule("rr:target.1")]
    public void APlayerWithAnEmptyHandIsOfferedOnlyTheThreat()
    {
        // rr:choose-option.1: "When an encounter card requires a player to choose an
        // option, they cannot choose an option that requires one or more targets if
        // there are no valid targets for that option." rr:target.1 lists "a card";
        // player 0's empty hand gives the random discard none. Player 1 holds a card
        // and keeps both options, and discards it.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var held = InHand(world, "01088", 1);
        var offered = new Dictionary<int, int>();

        var timestream = f.Reveal(Timestream);
        f.Finish(prompt =>
        {
            offered[prompt.Player] = prompt.Affordances.Count;
            if (prompt.Player == 0)
                Assert.Contains("threat", Assert.Single(prompt.Affordances).DisplayLabel, StringComparison.Ordinal);
            return Decision.Take(prompt.Affordances[0].Id);
        });

        Assert.Equal(1, offered[0]);
        Assert.Equal(2, offered[1]);
        Assert.Equal(2 + 2, timestream.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, held.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void AHeroActionOnAnObligationIsNotAnAlterEgoAction()
    {
        // Synthetic row: Stolen Memories prints a Hero Action instead. Corrupted
        // Timestream names only "Alter-Ego Action" abilities, so it stays offered.
        var root = JsonNode.Parse(Marvel.Content.Tests.Cards.AuthoredCards.Text)!;
        var row = root["cards"]!.AsArray().Single(card => (string?)card!["card"] == "11019")!;
        row["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{"discard":"this"}}]
            """);
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        var (world, _) = KangTemporalFixture.Board(players: 2);
        world.Abilities = runner;
        var f = new RedSkullFixture(world, runner);
        var memories = Obligation(world, "11019", 1);
        world.CreateCard(Timestream, world.AreaOf(DeckType.SideSchemesArea));

        Assert.True(Offers(f, memories, 1));
    }

    [Fact]
    [Rule("rr:cannot")]
    public void AnOfferedObligationActionIsRefusedOnceTimestreamArrives()
    {
        // The action was offered on an earlier board. Executing it after Corrupted
        // Timestream enters play is refused before the cost moves.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var memories = Obligation(world, "11019", 1);
        AlterEgo(world, 1);
        var genius = InHand(world, "01089", 1);
        var action = Assert.Single(f.Runner.Actions(world, 1), offer => offer.Card == memories.ObjectId);
        world.CreateCard(Timestream, world.AreaOf(DeckType.SideSchemesArea));

        Assert.Throws<RulesNotImplementedException>(() =>
            f.Runner.ExecuteAction(world, new PlayerAction(action, [], [genius.ObjectId])));
        Assert.Equal(DeckType.HandsArea, genius.Area.Type);
        Assert.Equal(DeckType.ObligationsArea, memories.Area.Type);
    }
}
