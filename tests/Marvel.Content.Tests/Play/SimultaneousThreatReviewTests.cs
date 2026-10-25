using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousThreatReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("trigger.subject")]
    [InlineData("trigger.target")]
    [Rule("rr:triggering-condition.1")]
    [Rule("rr:triggering-condition.2")]
    [Rule("rr:forced.6")]
    public void GenericResponseBindsEachPlacementThroughItsSuspendedChoice(string binding)
    {
        // Synthetic contract: each response triggers "once per occurrence" and
        // must "resolve as completely as possible before the next" initiates.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"game"},
              "effect":{"choose":{"options":[
                {"removeThreat":{"scheme":"BINDING","amount":1}},
                {"removeThreat":{"scheme":"BINDING","amount":2}}
              ]}}
            }]}]}
            """.Replace("BINDING", binding, StringComparison.Ordinal)));
        var (world, first, second) = Board(runner);
        var events = new List<GameEvent>();
        Threat.Schedule(world, new([first, second], null, 3, ThreatCause.CardAbility, "synthetic", 0));
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(2, order.Affordances.Count);
        Assert.Equal(2, order.Affordances.Select(offer => offer.Id).Distinct().Count());
        Assert.Equal(3, first.Tokens["k_threat"]);
        Assert.Equal(3, second.Tokens["k_threat"]);
        Sequence.Answer(world, Cards, runner, order, Decision.Take(order.Affordances[1].Id), events);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Option, choice.Asking);
        Assert.Equal(3, first.Tokens["k_threat"]);
        Assert.Equal(3, second.Tokens["k_threat"]);
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(choice.Affordances[0].Id), events);
        choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Option, choice.Asking);
        Assert.Equal(3, first.Tokens["k_threat"]);
        Assert.Equal(2, second.Tokens["k_threat"]);
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(choice.Affordances[1].Id), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(1, first.Tokens["k_threat"]);
        Assert.Equal(2, second.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:prevent.2")]
    [Rule("rr:replacement-effect.1")]
    public void PreventedOrReplacedAssignmentDoesNotCreateAPlacementResponse(bool replace)
    {
        // "Reduce the amount ... before it is placed"; a replacement resolves
        // "in place of" the occurrence, so only the second assignment responds.
        string effect = replace ? """{"replaceThreatWithDamage":{"card":"you"}}"""
            : """{"preventThreat":1}""";
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01007","abilities":[{
                "trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"game"},
                "effect":{"placeCounters":{"card":"trigger.target","counter":"seen","count":1}}}]},
              {"card":"01008","abilities":[{
                "trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"game"},
                "cost":{"exhaust":"this"},"effect":EFFECT}]}
            ]}
            """.Replace("EFFECT", effect, StringComparison.Ordinal)));
        var (world, first, second) = Board(runner);
        world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Threat.Schedule(world, new([first, second], null, 1, ThreatCause.CardAbility, "synthetic", 0));
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(0, first.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, first.Tokens.GetValueOrDefault("c_seen"));
        Assert.Equal(1, second.Tokens["k_threat"]);
        Assert.Equal(1, second.Tokens["c_seen"]);
        Assert.Equal(replace ? 1 : 0, world.Seats[0].IdentityCard.Damage);
    }

    private static (World World, Card First, Card Second) Board(AbilityRunner runner)
    {
        var world = Hero("01001a");
        world.Abilities = runner;
        world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        return (world, first, second);
    }
}
