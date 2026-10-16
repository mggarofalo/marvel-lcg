using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ChoiceResourcePaymentContinuationTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void APaidChoiceWaitsForItsGeneratorBeforeContinuingTheAbility()
    {
        // "Pay the cost(s)." The player's accepted pay branch must not become
        // the otherwise branch or repeat when its generator yields to an interrupt.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"seq":[
                  {"payOrExhaust":{"resources":"Y","otherwise":{"exhaust":"this"}}},
                  {"placeCounters":{"card":"this","counter":"test","count":1}}
                ]}
              }]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},
                "effect":{"generate":"Y"}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var pay = Assert.Single(game.Pending!.Affordances, option => option.CostOptions.Count > 0);
        int[] paying = [generator!.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(pay.Costs!), paying));

        game.Resolve(new Decision(pay.Id, [], paying, Allocations: allocation));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(0, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.True(source.Ready);

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(1, source.Tokens["c_test"]);
        Assert.True(source.Ready);
        Assert.False(world.Agenda.IsBusy);
    }
}
