using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkBasicReprintReviewTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void HelicarrierInEitherFormDiscountsAnOrdinarySupportAndSurvivesFailedPayment(
        int recipient, bool alterEgo)
    {
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"]);
        world.Seats[1].IdentityCard.TurnTo(alterEgo ? "01010b" : "01010a");
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10024",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
        var seat = world.Seats[recipient];
        var mansion = world.CreateCard("10023", seat.Hand);
        var doubleResource = world.CreateCard("10022", seat.Hand);
        var singleResource = world.CreateCard("01003", seat.Hand);
        var next = world.CreateCard("01083", seat.Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(seat.IdentityCard.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(source.Ready);

        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        for (int query = 0; query < 2; query++)
            Assert.Equal("3", CardPayment.Price(world, world.Facts, seat, mansion)!.Cost);
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.Play(world, world.Facts, runner, seat, mansion, [doubleResource.ObjectId], []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal("3", CardPayment.Price(world, world.Facts, seat, mansion)!.Cost);

        CardPlay.Play(world, world.Facts, runner, seat, mansion,
            [doubleResource.ObjectId, singleResource.ObjectId], []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Same(world.AreaOf(DeckType.SupportsArea, PlayArea.Of(recipient), cardOwner: recipient), mansion.Area);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(recipient), cardOwner: recipient), doubleResource.Area);
        Assert.Same(doubleResource.Area, singleResource.Area);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, seat, next).Amount);
    }
}
