using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowStageContinuationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:villain-defeat")]
    [Rule("rr:reveal.step.3")]
    [Rule("rr:and")]
    public void ActualRhinoPromotionFinishesSearchAndReturnsThePaidHammer(bool present)
    {
        // rr:villain-defeat: "The next sequential stage of the villain deck is revealed."
        // rr:reveal.step.3: "Resolve each When Revealed ability on that card."
        // rr:and: the two effects resolve simultaneously, before later responses.
        var runner = AuthoredCards.Runner();
        var probed = Prepare(runner, present);
        var control = Prepare(runner, present);
        var before = Snapshot(probed.World);
        var action = Assert.Single(runner.Actions(probed.World, 0), a => a.Card == probed.Event.ObjectId);
        for (int i = 0; i < 3; i++)
        {
            runner.Actions(probed.World, 0);
            runner.Describe(probed.World, action);
            Assert.Equal(before, Snapshot(probed.World));
        }
        foreach (var game in new[] { probed, control })
        {
            runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
            var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
            Assert.False(game.Hammer.Ready);
            Assert.Equal(DeckType.UpgradesArea, game.Hammer.Area.Type);
            Assert.All(game.Resources, id => Assert.Equal(DeckType.DiscardPile, game.World.Cards[id].Area.Type));
            var pending = Snapshot(game.World);
            runner.Describe(game.World, action);
            Assert.Equal(pending, Snapshot(game.World));
            Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Villain.ObjectId), []);
            Sequence.Finish(game.World, game.World.Facts, runner, []);
            Assert.Equal("01095", game.World.TheCardIn(DeckType.VillainArea)!.FaceId);
            Assert.Equal(0, game.World.TheCardIn(DeckType.VillainArea)!.Damage);
            Assert.Same(game.World.Seats[0].Hand, game.Hammer.Area);
            Assert.Equal(DeckType.DiscardPile, game.Event.Area.Type);
            if (game.Scheme is { } scheme)
            {
                Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
                Assert.Equal(3, scheme.Tokens["k_threat"]);
            }
            Assert.Empty(game.World.Agenda.Outstanding);
        }
        Assert.Equal(Snapshot(control.World), Snapshot(probed.World));
    }

    [Theory]
    [InlineData("01095", "{\"discard\":{\"titled\":\"Mjolnir\"}}")]
    [InlineData("01107", "{\"discard\":{\"titled\":\"Mjolnir\"}}")]
    [InlineData("01095", "{\"dealDamage\":{\"cards\":\"you\",\"amount\":1}}")]
    public void SourceChangingOrHealthChangingRevealIsRefusedBeforeDamage(string face, string effect)
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == face)!
            ["abilities"]![0]!["effect"] = JsonNode.Parse(effect);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var game = Prepare(runner, true);
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        var before = Snapshot(game.World);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Villain.ObjectId), []));
        Assert.Contains("unprojected When Revealed", error.Message);
        Assert.Equal(before, Snapshot(game.World));
    }

    [Fact]
    public void SchemeDependentHealthIsRefusedBeforeDamage()
    {
        // Synthetic constant: revealing the named scheme changes a character's
        // health, so the public reveal's otherwise harmless shape is insufficient.
        var runner = HammerThrowRecoveryPreflightTests.Runner(helmet: """
            {"if":{"test":{"titleInPlay":"Breakin' & Takin'"},
            "then":{"grant":{"card":"you","keyword":"health","amount":5}}}}
            """);
        var game = Prepare(runner, true);
        game.World.CreateCard("06010", game.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        var before = Snapshot(game.World);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Villain.ObjectId), []));
        Assert.Contains("unprojected When Revealed", error.Message);
        Assert.Equal(before, Snapshot(game.World));
    }
    private static Prepared Prepare(AbilityRunner runner, bool present)
    {
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(7);
        world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        var scheme = present ? world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck)) : null;
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        return new(world, villain, hammer, card, scheme, [first.ObjectId, second.ObjectId]);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new
        {
            step.What, step.Round, step.Number, step.Index, step.Subject, step.Seat,
            step.Plan, step.OccurrenceId, Operation = JsonSerializer.Serialize(step.Operation, step.Operation.GetType()),
        }),
    });

    private sealed record Prepared(World World, Card Villain, Card Hammer, Card Event, Card? Scheme, int[] Resources);
}

