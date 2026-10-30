using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic abilities on a prepared board; no expansion card is admitted.
public sealed class AllyEntryTimingIndependentReviewTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:initiating-abilities.1")]
    [Rule("rr:ally-limit")]
    [Rule("rr:initiating-abilities.2")]
    public void InterruptThenLimitThenResponseAreOneEntry(bool play)
    {
        // An interrupt may initiate before its condition becomes true; the ally
        // limit is immediate, and a response follows the triggering condition.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06012","abilities":[
              {"trigger":{"event":"WhenCardEntersPlay","timing":"Interrupt","subject":"this"},
               "effect":{"placeCounters":{"card":"this","counter":"before","count":1}}},
              {"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
               "effect":{"placeCounters":{"card":"this","counter":"after","count":1}}}
            ]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var old = world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var ally = world.CreateCard("06012", seat.Hand);
        var first = world.CreateCard("06022", seat.Hand);
        var second = world.CreateCard("01003", seat.Hand);
        if (play) CardPlay.Play(world, world.Facts, runner, seat, ally, [first.ObjectId, second.ObjectId], []);
        else CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);

        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(play ? DeckType.RevealingArea : DeckType.HandsArea, ally.Area.Type);
        var before = Assert.Single(interrupt.Affordances, offer => offer.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(before.Id), []);
        var limit = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(Steps.ChooseAllyForLimit, limit.Trigger);
        Assert.Equal(1, ally.Tokens.GetValueOrDefault("c_before"));
        Assert.Equal(0, ally.Tokens.GetValueOrDefault("c_after"));
        Sequence.Answer(world, world.Facts, runner, limit, Decision.Take(old.ObjectId), []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, old.Area.Type);
        var after = Assert.Single(response.Affordances, offer => offer.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(after.Id), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, ally.Tokens.GetValueOrDefault("c_before"));
        Assert.Equal(1, ally.Tokens.GetValueOrDefault("c_after"));
        Assert.Empty(world.Agenda.Outstanding);
    }
}
