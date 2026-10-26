using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowEncounterDealStageTests
{
    [Theory]
    [InlineData("04093", "04094")]
    [InlineData("04094", "04095")]
    [InlineData("04125", "04126")]
    [InlineData("04126", "04127")]
    [Rule("rr:villain-defeat")]
    [Rule("rr:reveal.step.3")]
    [Rule("rr:and")]
    public void ActualStageDealsFacedownCardsBeforeReturningThePaidHammer(string first, string next)
    {
        // rr:villain-defeat: "The next sequential stage of the villain deck is revealed."
        // rr:reveal.step.3: "Resolve each When Revealed ability on that card."
        // rr:and: the two effects resolve simultaneously, before later responses.
        var runner = AuthoredCards.Runner();
        var probed = Prepare(runner, first, next);
        var control = Prepare(runner, first, next);
        var before = Snapshot(probed.World);
        for (int i = 0; i < 3; i++)
        {
            var offered = Assert.Single(runner.Actions(probed.World, 0), a => a.Card == probed.Event.ObjectId);
            runner.Describe(probed.World, offered);
            Assert.Equal(before, Snapshot(probed.World));
        }
        foreach (var game in new[] { probed, control })
        {
            var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
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
            Assert.Equal(next, game.World.TheCardIn(DeckType.VillainArea)!.FaceId);
            Assert.Equal(0, game.World.TheCardIn(DeckType.VillainArea)!.Damage);
            Assert.Same(game.World.Seats[0].Hand, game.Hammer.Area);
            Assert.Equal(DeckType.DiscardPile, game.Event.Area.Type);
            Assert.Equal(11, game.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
            Assert.Single(game.World.Cards, card => card.Area.Type == DeckType.DealtEncounterCardsDeck);
            Assert.Empty(game.World.Agenda.Outstanding);
        }
        Assert.Equal(Snapshot(control.World), Snapshot(probed.World));
    }

    internal static Prepared Prepare(AbilityRunner runner, string first, string next, int players = 1)
    {
        var world = ThorActualFixture.Board("06001a", players);
        world.Abilities = runner;
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        var villain = world.CreateCard(first, world.AreaOf(DeckType.VillainArea));
        villain.TakeDamage(DamagePlacement.Health(world, world.Facts, villain) - 4);
        world.CreateCard(next, world.AreaOf(DeckType.VillainDeck));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var resource = world.CreateCard("01090", world.Seats[0].Hand);
        var other = world.CreateCard("01003", world.Seats[0].Hand);
        return new(world, villain, hammer, card, [resource.ObjectId, other.ObjectId]);
    }

    internal static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new
        {
            step.What, step.Round, step.Number, step.Index, step.Subject, step.Seat,
            step.Plan, step.OccurrenceId, Operation = JsonSerializer.Serialize(step.Operation, step.Operation.GetType()),
        }),
    });

    internal sealed record Prepared(World World, Card Villain, Card Hammer, Card Event, int[] Resources);
}


