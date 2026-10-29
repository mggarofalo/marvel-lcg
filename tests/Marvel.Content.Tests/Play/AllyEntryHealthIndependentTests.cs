using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AllyEntryHealthIndependentTests
{
    [Fact]
    [Rule("rr:hit-points.3")]
    [Rule("rr:interrupt.3.1")]
    public void DefeatInterruptCanIncreaseHealthAndTheSurvivingAllyGetsItsEntryResponse()
    {
        // A would-interrupt resolves before its imminent triggering condition;
        // an ally with zero remaining hit points is defeated.
        // Synthetic abilities make the counter increase health during that interrupt.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"12011","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"grant":{"card":"this","keyword":"health",
                 "amount":{"countersOn":{"card":"this","counter":"pym"}}}}},
              {"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
               "effect":{"placeCounters":{"card":"this","counter":"response","count":1}}}
            ]},{"card":"01092","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
              "effect":{"placeCounters":{"card":{"titled":"Ant-Man"},"counter":"pym","count":1}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var observer = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = world.CreateCard("12011", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "synthetic", []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var rescue = Assert.Single(interrupt.Affordances, item => item.AnchorId == observer.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(rescue.Id), []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(1, DamagePlacement.Health(world, world.Facts, ally));
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, ally.Tokens.GetValueOrDefault("c_response"));
    }
}
