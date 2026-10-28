using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic responses exercise a nested spend without admitting expansion content.
public sealed class SpentResourceNestedReviewTests
{
    [Fact]
    [Rule("rr:first-player.5")]
    [Rule("rr:response.4")]
    public void SimultaneousContributorsReceiveFirstPlayerPriorityInOneWindow()
    {
        // The first player has the first opportunity to use a response.
        var runner = SpentResourceResponseTests.Runner();
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var played = world.CreateCard("21019", world.Seats[1].Hand);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "alliance", 1, Affects: played.ObjectId));
        var first = world.CreateCard("12006", world.Seats[0].Hand);
        var second = world.CreateCard("12006", world.Seats[1].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[1], played, [first.ObjectId, second.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(0, prompt.Player);
        var response = Assert.Single(prompt.Affordances, item => item.AnchorId == first.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, prompt.Player);
        response = Assert.Single(prompt.Affordances, item => item.AnchorId == second.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_spent_seen"));
        Assert.Equal(1, world.Seats[1].IdentityCard.Tokens.GetValueOrDefault("c_spent_seen"));
        Assert.Equal(DeckType.AlliesArea, played.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void ReenteredSpentCopyCannotAnswerItsEarlierOccurrence()
    {
        // A card that leaves play is considered "a new copy of the card".
        var runner = SpentResourceResponseTests.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var played = world.CreateCard("21019", seat.Hand);
        var first = world.CreateCard("12006", seat.Hand);
        var second = world.CreateCard("12006", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, played, [first.ObjectId, second.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var response = Assert.Single(prompt.Affordances, item => item.AnchorId == first.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        // Synthetic movement isolates the window's copy identity check.
        var discard = second.Area;
        World.MoveToTop(second, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        World.MoveToTop(second, discard);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, seat.IdentityCard.Tokens.GetValueOrDefault("c_spent_seen"));
        Assert.Equal(DeckType.AlliesArea, played.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.6")]
    public void ResponsePaymentOpensItsOwnWindowBeforeEitherEffectOrEntry()
    {
        // Step 5 pays costs before step 6 initiates an ability or card.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
          {"cards":[
            {"card":"12006","abilities":[{
              "trigger":{"event":"WhenResourceCardSpent","timing":"Response","subject":"this","player":"you"},
              "cost":{"spend":"Y"},"effect":{"placeCounters":{"card":"you","counter":"outer","count":1}}}]},
            {"card":"01088","abilities":[{
              "trigger":{"event":"WhenResourceCardSpent","timing":"Response","subject":"this","player":"you"},
              "effect":{"placeCounters":{"card":"you","counter":"inner","count":1}}}]}]}
          """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var played = world.CreateCard("21019", seat.Hand);
        var outer = world.CreateCard("12006", seat.Hand);
        var inner = world.CreateCard("01088", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, played, [outer.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var response = Assert.Single(prompt.Affordances, item => item.AnchorId == outer.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, PaidDecision.Take(response, [inner.ObjectId]), []);
        prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.NotEqual(DeckType.AlliesArea, played.Area.Type);
        Assert.Equal(0, seat.IdentityCard.Tokens.GetValueOrDefault("c_outer"));
        response = Assert.Single(prompt.Affordances, item => item.AnchorId == inner.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, seat.IdentityCard.Tokens.GetValueOrDefault("c_inner"));
        Assert.Equal(1, seat.IdentityCard.Tokens.GetValueOrDefault("c_outer"));
        Assert.Equal(DeckType.AlliesArea, played.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
