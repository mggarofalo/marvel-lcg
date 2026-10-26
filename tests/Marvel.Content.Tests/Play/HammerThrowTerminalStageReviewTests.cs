using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.HammerThrowEncounterDealStageTests;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowTerminalStageReviewTests
{
    [Fact]
    [Rule("rr:villain-defeat")]
    [Rule("rr:reveal.step.3")]
    [Rule("rr:overkill.1")]
    public void OverkillPromotionWithExistingSuperAbsorbingPowerDealsToBothPlayersWithoutSearching()
    {
        // Printed 04077: "If Super Absorbing Power is in play, deal 1 encounter card to each player."
        // rr:villain-defeat: "The next sequential stage of the villain deck is revealed."
        // rr:reveal.step.3: "Resolve each When Revealed ability on that card."
        // rr:overkill.1: "If this attack defeats a minion, deal the damage beyond its hit points to the villain."
        var runner = AuthoredCards.Runner();
        var game = Prepare(runner, "04076", "04077", players: 2);
        var environment = game.World.CreateCard("04092", game.World.AreaOf(DeckType.EnvironmentArea));
        var untouched = game.World.CreateCard("04092", game.World.AreaOf(DeckType.EncounterDeck));
        var minion = game.World.CreateCard("01101", game.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Equal(3, DamagePlacement.Health(game.World, game.World.Facts, minion));
        var before = Snapshot(game.World);
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.Describe(game.World, action);
        Assert.Equal(before, Snapshot(game.World));
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(minion.ObjectId), []);
        Sequence.Finish(game.World, game.World.Facts, runner, []);
        Assert.Equal("04077", game.World.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.Equal(0, game.World.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.EnvironmentArea, environment.Area.Type);
        Assert.Equal(DeckType.DealtEncounterCardsDeck, untouched.Area.Type);
        var dealt = game.World.Cards.Where(c => c.Area.Type == DeckType.DealtEncounterCardsDeck).ToArray();
        Assert.Equal(2, dealt.Length);
        Assert.Single(dealt, c => c.Area.PlayArea.Player == 0);
        Assert.Single(dealt, c => c.Area.PlayArea.Player == 1);
        Assert.Equal(11, game.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Same(game.World.Seats[0].Hand, game.Hammer.Area);
        Assert.Empty(game.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(12, false)]
    [InlineData(16, true)]
    [Rule("rr:hit-points.2.3")]
    public void TerminalReturnSettlesItsOwnersHealthLossAfterStageEntry(int damage, bool eliminated)
    {
        // Synthetic helmet condition isolates the live return's health settlement.
        // rr:hit-points.2.3: "If that ability later ceases to be in effect, reduce that character's hit point dial by X."
        var runner = HammerThrowRecoveryPreflightTests.Runner(helmet: """
            {"if":{"test":{"hasTrait":{"card":"you","trait":"AERIAL"}},
            "then":{"grant":{"card":"you","keyword":"health","amount":5}}}}
            """);
        var game = Prepare(runner, "01094", "01095", players: 2);
        game.World.CreateCard("06010", game.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var hero = game.World.Seats[0].IdentityCard;
        hero.TakeDamage(damage);
        Assert.Equal(19, DamagePlacement.Health(game.World, game.World.Facts, hero));
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Villain.ObjectId), []);
        Sequence.Finish(game.World, game.World.Facts, runner, []);
        Assert.Equal("01095", game.World.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.Equal(eliminated, game.World.Seats[0].Eliminated);
        Assert.False(game.World.Seats[1].Eliminated);
        Assert.Equal(eliminated ? DeckType.RemovedArea : DeckType.HandsArea, game.Hammer.Area.Type);
        Assert.Empty(game.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("{\"seq\":[{\"returnToHand\":{\"titled\":\"Mjolnir\"}}]}")]
    [InlineData("{\"if\":{\"test\":{\"titleInPlay\":\"Mjolnir\"},\"then\":{\"returnToHand\":{\"titled\":\"Mjolnir\"}}}}")]
    public void NestedReturnCannotBypassAnOuterFollowingBoardRead(string nested)
    {
        // Synthetic power: the return ends its nested branch, but not the containing power.
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var rows = book["cards"]!.AsArray();
        rows.Single(row => row!["card"]!.GetValue<string>() == "01095")!["abilities"]![0]!["effect"] =
            JsonNode.Parse("""{"discard":{"titled":"Mjolnir"}}""");
        var sequence = rows.Single(row => row!["card"]!.GetValue<string>() == "06005")!
            ["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["attack"]!["effect"]!["seq"]!.AsArray();
        sequence[1] = JsonNode.Parse(nested);
        sequence.Add(JsonNode.Parse("""{"dealDamage":{"cards":"you","amount":1}}"""));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var game = Prepare(runner, "01094", "01095");
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        var before = Snapshot(game.World);
        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(
            game.World, game.World.Facts, runner, target, Decision.Take(game.Villain.ObjectId), []));
        Assert.Contains("unprojected When Revealed", error.Message);
        Assert.Equal(before, Snapshot(game.World));
    }
}
