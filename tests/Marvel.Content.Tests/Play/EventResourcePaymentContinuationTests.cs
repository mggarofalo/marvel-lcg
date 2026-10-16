using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventResourcePaymentContinuationTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void AnEventsPaymentAndEffectWaitForItsResourceAbility()
    {
        // "Pay the cost(s)." A synthetic generator supplies the printed event cost
        // only after the interrupt to its lethal self-damage cost has finished.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01060","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
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
        var (game, world) = Playing(board =>
        {
            eventCard = board.CreateCard("01060", board.Seats[0].Hand);
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == eventCard!.ObjectId);
        int[] paying = [generator!.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(action.Costs!), paying));

        game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.RevealingArea, eventCard!.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
        Assert.DoesNotContain(world.Effects.Active(), effect =>
            effect.Card == eventCard.ObjectId && effect.Kind.StartsWith("paid:", StringComparison.Ordinal));

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.False(world.Agenda.IsBusy);
    }
}
