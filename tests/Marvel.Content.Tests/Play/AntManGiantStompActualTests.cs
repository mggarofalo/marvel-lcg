using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AntManGiantStompActualTests
{
    private const string Tail = """{"attackAnyEnemy":{"amount":8}}""";

    [Theory]
    [InlineData("12001a", false)]
    [InlineData("12001b", false)]
    [InlineData("12001c", true)]
    public void PrintedGiantHeroRestrictionControlsOffer(string face, bool offered)
    {
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = GiantStompCompositionTests.Board(runner, 0);
        world.Seats[0].IdentityCard.TurnTo(face);
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
    }

    [Fact]
    [Rule("rr:ability.4")]
    public void SplashReachesMinionsEngagedWithBothPlayersBeforeTargetChoice()
    {
        // Resolve the ability "one sentence at a time".
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo("12001c");
        world.Abilities = runner;
        var first = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var source = world.CreateCard("12003", seat.Hand);
        int[] payment = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01087", seat.Hand).ObjectId).ToArray();
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, first.Damage);
        Assert.Equal(1, second.Damage);
        Assert.Equal(1, prompt.Player);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(villain.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(8, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }
}
