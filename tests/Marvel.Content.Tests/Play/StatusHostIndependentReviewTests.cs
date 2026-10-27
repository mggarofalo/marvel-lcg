using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic controls for the bounded physical-status component binding.</summary>
public sealed class StatusHostIndependentReviewTests
{
    private const string Combined = """
        {"and":[{"heal":{"card":"chosen","amount":1}},
          {"chooseCard":{"from":{"statusCardsOn":"chosen"},"effect":{"discard":"chosen"}}}]}
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:and.1")]
    public void NestedHeroChoiceRetainsItsOwnBindingAcrossComponentChoiceAndOuterSuffix(bool statusFirst)
    {
        // "Individual effects connected by 'and' are not dependent on each other."
        // Local component binding and ordinary nested chosen persistence are engine choices.
        string inner = "{\"chooseCard\":{\"from\":{\"query\":\"heroes\"},\"effect\":" + Combined + "}}";
        string body = "{\"chooseCard\":{\"from\":{\"query\":\"heroes\"},\"effect\":{\"seq\":["
            + inner + ", {\"heal\":{\"card\":\"chosen\",\"amount\":1}},"
            + "{\"draw\":{\"player\":\"chosenPlayer\",\"count\":1}}]}}}";
        var (world, runner, source) = Board(body);
        var outer = world.Seats[0].IdentityCard;
        var innerHero = world.Seats[1].IdentityCard;
        outer.TakeDamage(3);
        innerHero.TakeDamage(3);
        var status = Statuses.Give(world, innerHero, Statuses.Tough);
        var events = runner.ExecuteAction(world, new PlayerAction(
            Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId), [], [])).Events.ToList();
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(outer.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(innerHero.ObjectId), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances).Id, statusFirst ? [1, 0] : [0, 1]), events);
        prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal([status.ObjectId], prompt.Affordances.Select(item => item.AnchorId));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(status.ObjectId), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(3, outer.Damage);
        Assert.Equal(1, innerHero.Damage);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.Equal(DeckType.RemovedArea, status.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(string body)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},"effect":{{{body}}}
            }]}]}
            """));
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        return (world, runner, source);
    }

}
