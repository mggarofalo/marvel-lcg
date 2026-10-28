using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored Stinger on a prepared board; this does not admit the pack.
public sealed class StingerActualIndependentReviewTests
{
    [Fact]
    [Rule("rr:ally-limit")]
    public void ChoosingStingerDoesNotResolveAnotherAllysExcess()
    {
        // A player above the limit must choose allies "until they have a number of allies in play equal to their ally limit."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var stinger = world.CreateCard("12014", area);
        var first = world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var blade = world.CreateCard("21019", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], blade, [payment.ObjectId], events);
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
        Assert.Contains(prompt.Affordances, option => option.Id == stinger.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(stinger.ObjectId), events);
        Assert.Equal(DeckType.DiscardPile, stinger.Area.Type);
        prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
        Assert.Contains(prompt.Affordances, option => option.Id == first.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(first.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.AlliesArea, blade.Area.Type);
    }
}
