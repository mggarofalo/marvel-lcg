using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WindowResourcePaymentContinuationTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void AnAcceptedInterruptWaitsForItsResourceCostBeforeResolving()
    {
        // "Pay the cost(s)." Synthetic trigger text isolates the nested payment
        // boundary while retaining actual Core card kinds, costs and health.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01060","abilities":[{
                "trigger":{"event":"Probe","timing":"Interrupt","subject":"game"},
                "effect":{"placeCounters":{"card":"you","counter":"test","count":1}}
              }]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},
                "effect":{"generate":"YY"}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? eventCard = null;
        Card? generator = null;
        var (_, world) = Playing(board =>
        {
            eventCard = board.CreateCard("01060", board.Seats[0].Hand);
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var result = runner.ResolveWindow(world, new Occurrence(1, ["Probe"], Player: 0),
            new PendingAbility(eventCard!.ObjectId, AbilityType.Interrupt, 0),
            Decision.Take(eventCard.ObjectId, [], [generator!.ObjectId]));
        var events = new List<GameEvent>(result.Events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));

        Assert.False(result.IsComplete);
        Assert.Equal(Question.Opportunity, prompt.Asking);
        Assert.Equal(DeckType.RevealingArea, eventCard.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));

        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);

        Assert.True(result.IsComplete);
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.False(world.Agenda.IsBusy);
    }
}
