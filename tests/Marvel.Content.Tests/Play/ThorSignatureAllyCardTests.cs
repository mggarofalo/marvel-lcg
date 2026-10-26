using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic supported boards exercise actual rows in the complete ability book.
public sealed class ThorSignatureAllyCardTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void HerculesPricingAndRejectedPaymentPreserveStateBeforeSuccessfulRetry()
    {
        // Step 5: "abort this process without paying any costs".
        var probed = ThorActualFixture.Board("06001a");
        var control = ThorActualFixture.Board("06001a");
        foreach (var world in new[] { probed, control })
        {
            var runner = AuthoredCards.Runner();
            var ally = world.CreateCard("06011", world.Seats[0].Hand);
            for (int i = 0; i < 2; i++)
                world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            var payment = new[] { world.CreateCard("06022", world.Seats[0].Hand),
                world.CreateCard("06023", world.Seats[0].Hand) };
            if (ReferenceEquals(world, probed))
            {
                var before = world.Digest().Canonical();
                var random = world.Random.Generator.GetState();
                var words = world.Random.Generator.WordsConsumed;
                var pending = world.Agenda.Outstanding.ToArray();
                var effects = world.Effects.Registered.ToArray();
                for (int i = 0; i < 3; i++)
                {
                    Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[0], ally).Amount);
                    Assert.Equal("4", CardPayment.Price(world, world.Facts, world.Seats[0], ally)!.Cost);
                }
                Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts,
                    runner, world.Seats[0], ally, [payment[0].ObjectId], []));
                Assert.Equal(before, world.Digest().Canonical());
                Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(words, world.Random.Generator.WordsConsumed);
                Assert.Equal(pending, world.Agenda.Outstanding);
                Assert.Equal(effects, world.Effects.Registered);
            }
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], ally,
                payment.Select(c => c.ObjectId).ToArray(), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
            Assert.Empty(world.Agenda.Outstanding);
        }
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(2, 4)]
    [InlineData(7, 0)]
    public void HerculesPaysLiveOwnEngagementPrice(int count, int expected)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[1];
        var ally = world.CreateCard("06011", seat.Hand);
        for (int i = 0; i < count; i++)
            world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var payment = Enumerable.Range(0, expected).Select(_ => world.CreateCard("01003", seat.Hand)).ToArray();
        Assert.Equal(expected, CardPayment.CostOf(world, world.Facts, seat, ally).Amount);
        CardPlay.Play(world, world.Facts, runner, seat, ally, payment.Select(c => c.ObjectId).ToArray(), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
        Assert.All(payment, c => Assert.Equal(DeckType.DiscardPile, c.Area.Type));
        Assert.Empty(world.Agenda.Outstanding);
    }

}
