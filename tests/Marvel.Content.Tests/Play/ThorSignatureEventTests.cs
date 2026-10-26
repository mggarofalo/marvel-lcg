using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorSignatureEventTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:search.1")]
    [Rule("rr:search.3")]
    public void ForAsgardPaysOneAndChoosesOwnPhysicalTraitMatch(bool discard)
    {
        // "the player chooses among those options"; "shuffle that entire deck."
        var world = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var first = world.CreateCard("06009", seat.Deck);
        var second = world.CreateCard("06007", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var foreign = world.CreateCard("06009", world.Seats[1].Deck);
        var card = world.CreateCard("06004", seat.Hand);
        var paying = world.CreateCard("01003", seat.Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        long before = world.Random.Generator.WordsConsumed;
        runner.ExecuteAction(world, new PlayerAction(action, [paying.ObjectId], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }.Order(), prompt.Affordances.Select(a => a.AnchorId).Order());
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == foreign.ObjectId);
        var chosen = discard ? second : first;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(chosen.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Same(seat.Hand, chosen.Area);
        Assert.Equal(DeckType.DiscardPile, paying.Area.Type);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.True(world.Random.Generator.WordsConsumed > before);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ForAsgardNoMatchStillPaysAndShufflesButHeroCannotPlay()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard("06004", world.Seats[0].Hand);
        var paying = world.CreateCard("01003", world.Seats[0].Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("06001b");
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        long before = world.Random.Generator.WordsConsumed;
        runner.ExecuteAction(world, new PlayerAction(action, [paying.ObjectId], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.True(world.Random.Generator.WordsConsumed > before);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Fact]
    public void EventQueriesAndRejectedPaymentPreserveRandomAndContinuationAgainstControl()
    {
        var probed = ThorActualFixture.Board("06001b");
        var control = ThorActualFixture.Board("06001b");
        var runner = AuthoredCards.Runner();
        foreach (var world in new[] { probed, control })
        {
            world.CreateCard("06004", world.Seats[0].Hand);
            world.CreateCard("01003", world.Seats[0].Hand);
        }
        var card = probed.Seats[0].Hand.Cards[0];
        var before = probed.Digest().Canonical();
        var random = probed.Random.Generator.GetState();
        var pending = probed.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 3; i++)
            Assert.Single(runner.Actions(probed, 0), a => a.Card == card.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(probed, probed.Facts, runner, probed.Seats[0], card, [], []));
        Assert.Equal(before, probed.Digest().Canonical());
        Assert.Equal(random.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(pending, probed.Agenda.Outstanding);
        foreach (var world in new[] { probed, control })
        {
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [world.Seats[0].Hand.Cards[1].ObjectId], []));
            Sequence.Finish(world, world.Facts, runner, []);
        }
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Agenda.Outstanding, probed.Agenda.Outstanding);
    }
}



