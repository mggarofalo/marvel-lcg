using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GiantStompLiveIncarnationTests
{
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void ReturningMinionCannotAnswerItsEarlierOffer()
    {
        // A card that leaves play has "no memory of its previous state".
        var runner = GiantStompCompositionTests.Runner("""{"attackAnyEnemy":{"amount":8}}""", targetless: true);
        var world = GiantStompCompositionTests.Board(runner, 1);
        var minion = world.Cards.Single(card => card.FaceId == "01102");
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var game = Game.Begin(world, world.Facts, runner);
        while (game.Pending!.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(game.Pending.Affordances.Single(option => option.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending.Affordances, offer => offer.AnchorId == source.ObjectId);
        int[] payment = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023")
            .Select(card => card.ObjectId).ToArray();
        game.Resolve(new Decision(action.Id, [], payment,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), payment)));
        var pending = game.Pending!;
        Assert.Contains(pending.Affordances, offer => offer.Id == minion.ObjectId);
        int incarnation = minion.Incarnation;
        var area = minion.Area;
        World.MoveToTop(minion, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(minion, area);
        Assert.NotEqual(incarnation, minion.Incarnation);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(minion.ObjectId)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Same(pending, game.Pending);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(8, villain.Damage);
    }
}
