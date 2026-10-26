using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowRecoveryReviewTests
{
    [Theory]
    [InlineData("01186")]
    [InlineData("01101")]
    public void RepeatedQueriesPreserveRandomPendingWorkAndTheUnprobedOutcome(string top)
    {
        // Synthetic board with actual Hammer Throw and Loki. The encounter top
        // stays opaque until the live would-be-defeated interrupt discards it.
        var runner = HammerThrowRecoveryPreflightTests.Runner();
        var probed = Prepare(runner, top);
        var control = Prepare(runner, top);
        var before = Snapshot(probed.World);
        var action = Assert.Single(runner.Actions(probed.World, 0), a => a.Card == probed.Event.ObjectId);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Assert.Equal(action, Assert.Single(runner.Actions(probed.World, 0), a => a.Card == probed.Event.ObjectId));
            runner.Describe(probed.World, action);
            Assert.Equal(before, Snapshot(probed.World));
        }
        foreach (var game in new[] { probed, control })
        {
            // Identical physical ids let the untouched control use this offer.
            runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
            var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
            var pending = Snapshot(game.World);
            runner.Describe(game.World, action);
            Assert.Equal(pending, Snapshot(game.World));
            Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Loki.ObjectId), []);
            Sequence.Finish(game.World, game.World.Facts, runner, []);
            Assert.Same(game.World.Seats[0].Hand, game.Hammer.Area);
            Assert.Equal(top == "01186" ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, game.Loki.Area.Type);
            Assert.Empty(game.World.Agenda.Outstanding);
        }
        Assert.Equal(Snapshot(control.World), Snapshot(probed.World));
    }

    [Theory]
    [InlineData("01186")]
    [InlineData("01101")]
    public void RecoveryRefusesAnAmbiguousPhysicalReturnWithoutChangingPendingWork(string top)
    {
        // Synthetic duplicate physical copies challenge title selection across
        // players; this is not an ordinary legal unique-card tableau.
        var runner = HammerThrowRecoveryPreflightTests.Runner();
        var game = Prepare(runner, top, foreignHammer: true);
        var foreign = Assert.Single(game.World.Cards, card => card.FaceId == "06009" && card.Owner == 1);
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        var before = Snapshot(game.World);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Loki.ObjectId), []));
        Assert.Contains("unprojected defeat window", error.Message);
        Assert.Equal(before, Snapshot(game.World));
        Assert.Equal(DeckType.UpgradesArea, game.Hammer.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, foreign.Area.Type);
        Assert.True(foreign.Ready);
        Assert.NotEmpty(game.World.Agenda.Outstanding);
    }

    private static Prepared Prepare(AbilityRunner runner, string top, bool foreignHammer = false)
    {
        var world = ThorActualFixture.Board("06001a", players: 2);
        world.Abilities = runner;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        if (foreignHammer)
            world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard(top, world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        return new(world, loki, hammer, card, [first.ObjectId, second.ObjectId]);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new
        {
            step.What, step.Round, step.Number, step.Index, step.Subject, step.Seat,
            step.Plan, step.OccurrenceId, Operation = JsonSerializer.Serialize(step.Operation, step.Operation.GetType()),
        }),
    });

    private sealed record Prepared(World World, Card Loki, Card Hammer, Card Event, int[] Resources);
}
