using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorSearchCardTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorthyChoosesPhysicalOwnDeckOrDiscardAndUsesOneRound(bool discard)
    {
        var world = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var deckCopy = world.CreateCard("06009", seat.Deck);
        var discardCopy = world.CreateCard("06009", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var foreign = world.CreateCard("06009", world.Seats[1].Deck);
        var inPlay = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
        output.WriteLine("WORTHY OFFER: " + runner.Describe(world, action).DisplayLabel + " | " + runner.Describe(world, action).Description);
        long before = world.Random.Generator.WordsConsumed;
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        output.WriteLine("WORTHY PROMPT: " + prompt.DisplayQuestion + " | " + prompt.Description);
        Assert.Equal(new[] { deckCopy.ObjectId, discardCopy.ObjectId }.Order(), prompt.Affordances.Select(a => a.AnchorId).Order());
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == foreign.ObjectId || a.AnchorId == inPlay.ObjectId);
        var chosen = discard ? discardCopy : deckCopy;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(chosen.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Same(seat.Hand, chosen.Area);
        Assert.True(world.Random.Generator.WordsConsumed > before);
        seat.IdentityCard.TurnTo("06001a");
        seat.IdentityCard.TurnTo("06001b");
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
        PhaseEnd.EndVillainPhase(world, world.Facts, []);
        Assert.Contains(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
    }

    [Fact]
    public void MissingMjolnirStillShufflesAndConsumesUse()
    {
        var world = ThorActualFixture.Board("06001b");
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        world.CreateCard("06009", seat.Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
        output.WriteLine("WORTHY OFFER: " + runner.Describe(world, action).DisplayLabel + " | " + runner.Describe(world, action).Description);
        long before = world.Random.Generator.WordsConsumed;
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(world.Random.Generator.WordsConsumed > before);
        Assert.Single(seat.Hand.Cards);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
    }
}
