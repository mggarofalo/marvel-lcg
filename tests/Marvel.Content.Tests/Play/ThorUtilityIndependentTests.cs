using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorUtilityIndependentTests : CoreCardTestBase
{
    [Fact]
    public void SecondWindDoesNotInheritMentalFromAnEarlierCardPayment()
    {
        // 06033: "5 damage instead if you paid for this card using a mental resource".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var physique = world.CreateCard("06034", seat.Hand);
        var mental = world.CreateCard("06023", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, physique, [mental.ObjectId], []);
        Sequence.Finish(world, world.Facts, runner, []);
        var card = world.CreateCard("06033", seat.Hand);
        var energy = world.CreateCard("06022", seat.Hand);
        var physical = world.CreateCard("01003", seat.Hand);
        seat.IdentityCard.TakeDamage(7);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, physical.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(seat.IdentityCard.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, seat.IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(3, physique.Tokens["c_physical"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:uses-x-type")]
    public void PhysiquesFinalResourceSurvivesSecondWindsChoiceAndInvalidRetry()
    {
        // "When the last all-purpose counter is removed from a card with uses, discard that card."
        var probed = ThorActualFixture.Board("06001a", 2);
        var control = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        foreach (var world in new[] { probed, control })
        {
            var seat = world.Seats[0];
            var source = world.CreateCard("06034", seat.Hand);
            var payment = world.CreateCard("06022", seat.Hand);
            CardPlay.Play(world, world.Facts, runner, seat, source, [payment.ObjectId], []);
            Sequence.Finish(world, world.Facts, runner, []);
            for (int i = 0; i < 2; i++)
            {
                var support = world.CreateCard("01026", seat.Hand);
                CardPlay.Play(world, world.Facts, runner, seat, support, [source.ObjectId], []);
                Sequence.Finish(world, world.Facts, runner, []);
                source.Refresh();
            }
            Assert.Equal(1, source.Tokens["c_physical"]);
            var card = world.CreateCard("06033", seat.Hand);
            var doubleResource = world.CreateCard("06022", seat.Hand);
            world.Seats[1].IdentityCard.TurnTo("01001b");
            world.Seats[1].IdentityCard.TakeDamage(7);
            var before = world.Digest().Canonical();
            var random = world.Random.Generator.GetState();
            var pending = world.Agenda.Outstanding.ToArray();
            if (ReferenceEquals(world, probed))
            {
                for (int i = 0; i < 3; i++)
                    Assert.Equal("R", Assert.Single(CardPayment.Generators(world, world.Facts, seat, runner, card), g => g.Effect == source.ObjectId).Generates);
                Assert.Equal(before, world.Digest().Canonical());
                Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(pending, world.Agenda.Outstanding);
            }
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [source.ObjectId, doubleResource.ObjectId], []));
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Assert.Equal(DeckType.DiscardPile, source.Area.Type);
            Assert.DoesNotContain(runner.ResourceAbilities(world, 0), g => g.Effect == source.ObjectId);
            if (ReferenceEquals(world, probed))
            {
                before = world.Digest().Canonical();
                random = world.Random.Generator.GetState();
                pending = world.Agenda.Outstanding.ToArray();
                Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(source.ObjectId), []));
                Assert.Equal(before, world.Digest().Canonical());
                Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(pending, world.Agenda.Outstanding);
            }
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(world.Seats[1].IdentityCard.ObjectId), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
            Assert.Equal(DeckType.DiscardPile, card.Area.Type);
            Assert.Empty(world.Agenda.Outstanding);
        }
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Agenda.Outstanding, probed.Agenda.Outstanding);
    }

    [Fact]
    public void SecondSeatMansionControllerChoosesFirstSeatsDraw()
    {
        // 06025: "choose a player. That player draws 1 card."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var mansion = world.CreateCard("06025", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == mansion.ObjectId);
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == mansion.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, prompt.Player);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(world.Seats[0].IdentityCard.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.False(mansion.Ready);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
