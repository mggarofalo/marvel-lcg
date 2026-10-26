using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorUtilitySupportTests : CoreCardTestBase
{
    [Theory]
    [InlineData("06001a")]
    [InlineData("06001b")]
    public void MansionExhaustsAndChoosesTheOtherPlayersDraw(string form)
    {
        var world = ThorActualFixture.Board(form, 2);
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard("06025", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.False(card.Ready);
        Assert.Equal(world.Seats.Select(s => s.IdentityCard.ObjectId).Order(), prompt.Affordances.Select(a => a.AnchorId).Order());
        var before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(card.ObjectId), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(world.Seats[1].IdentityCard.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == card.ObjectId);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:uses-x-type")]
    public void PhysiqueActualPlayStartsThreeCountersAndEachUsePaysIncludingDeparture()
    {
        // "When the last all-purpose counter is removed from a card with uses, discard that card."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var source = world.CreateCard("06034", seat.Hand);
        var payment = world.CreateCard("06022", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, source, [payment.ObjectId], []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, source.Tokens["c_physical"]);
        for (int uses = 3; uses > 0; uses--)
        {
            var cheap = world.CreateCard("01026", seat.Hand);
            var before = world.Digest().Canonical();
            var rng = world.Random.Generator.GetState();
            var pending = world.Agenda.Outstanding.ToArray();
            for (int i = 0; i < 2; i++)
                Assert.Equal("R", Assert.Single(CardPayment.Generators(world, world.Facts, seat, runner, cheap), g => g.Effect == source.ObjectId).Generates);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(pending, world.Agenda.Outstanding);
            Assert.DoesNotContain(runner.ResourceAbilities(world, 1), g => g.Effect == source.ObjectId);
            seat.IdentityCard.TurnTo("06001b");
            Assert.DoesNotContain(runner.ResourceAbilities(world, 0), g => g.Effect == source.ObjectId);
            seat.IdentityCard.TurnTo("06001a");
            CardPlay.Play(world, world.Facts, runner, seat, cheap, [source.ObjectId], []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(DeckType.SupportsArea, cheap.Area.Type);
            Assert.Equal(uses == 1 ? DeckType.DiscardPile : DeckType.UpgradesArea, source.Area.Type);
            Assert.DoesNotContain(runner.ResourceAbilities(world, 0), g => g.Effect == source.ObjectId);
            if (uses > 1)
            {
                Assert.Equal(uses - 1, source.Tokens["c_physical"]);
                Assert.False(source.Ready);
                source.Refresh();
            }
        }
        Assert.Empty(world.Agenda.Outstanding);
    }
    [Theory]
    [InlineData("06025")]
    [InlineData("06033")]
    public void QueriesAndRejectedRetryMatchAnUnprobedControl(string face)
    {
        var probed = ThorActualFixture.Board("06001b", 2);
        var control = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        foreach (var world in new[] { probed, control })
        {
            world.Seats[1].IdentityCard.TakeDamage(6);
            var area = face == "06025" ? world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0) : world.Seats[0].Hand;
            world.CreateCard(face, area);
            world.CreateCard("06022", world.Seats[0].Hand);
            world.CreateCard("01005", world.Seats[0].Hand);
        }
        var card = probed.Cards.Single(c => c.FaceId == face);
        var before = probed.Digest().Canonical();
        var rng = probed.Random.Generator.GetState();
        var pending = probed.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 3; i++)
            Assert.Single(runner.Actions(probed, 0), a => a.Card == card.ObjectId);
        Assert.Equal(before, probed.Digest().Canonical());
        Assert.Equal(rng.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(pending, probed.Agenda.Outstanding);
        foreach (var world in new[] { probed, control })
        {
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
            int[] paying = face == "06033" ? world.Seats[0].Hand.Cards.Where(c => c.FaceId != face).Select(c => c.ObjectId).ToArray() : [];
            if (ReferenceEquals(world, probed) && face == "06033")
            {
                Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
                Assert.Equal(before, world.Digest().Canonical());
                Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(pending, world.Agenda.Outstanding);
            }
            runner.ExecuteAction(world, new PlayerAction(action, paying, []));
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(world.Seats[1].IdentityCard.ObjectId), []);
            Sequence.Finish(world, world.Facts, runner, []);
        }
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Agenda.Outstanding, probed.Agenda.Outstanding);
    }

    [Fact]
    public void MansionPaysFourAndSharesThePerPlayerTitleLimitWithCorePrinting()
    {
        var world = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var mansion = world.CreateCard("06025", seat.Hand);
        var energy = world.CreateCard("06022", seat.Hand);
        var physical = world.CreateCard("06024", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, mansion, [energy.ObjectId, physical.ObjectId], []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.SupportsArea, mansion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, physical.Area.Type);
        var duplicate = world.CreateCard("01091", seat.Hand);
        world.CreateCard("06022", seat.Hand);
        world.CreateCard("06024", seat.Hand);
        Assert.Null(CardPayment.Price(world, world.Facts, seat, duplicate));
        var other = world.CreateCard("06025", world.Seats[1].Hand);
        world.CreateCard("06022", world.Seats[1].Hand);
        world.CreateCard("06024", world.Seats[1].Hand);
        Assert.NotNull(CardPayment.Price(world, world.Facts, world.Seats[1], other));
    }

}
