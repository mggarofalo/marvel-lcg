using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventDiscardPaymentContinuationTests
{
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:leaves-play.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADiscardCostWaitsForTheEventsSuspendedResourcePayment(bool reenter)
    {
        // "Pay the cost(s)." This synthetic resource ability suspends at its
        // lethal damage cost, before the event may pay its remaining discard.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01060","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"discardFromHand":1},
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
        Card? discarded = null;
        var (game, world) = Playing(board =>
        {
            eventCard = board.CreateCard("01060", board.Seats[0].Hand);
            discarded = board.CreateCard("01093", board.Seats[0].Hand);
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == eventCard!.ObjectId);
        int[] paying = [generator!.ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), paying);
        game.Resolve(new Decision(action.Id, [discarded!.ObjectId], paying, Allocations: allocation));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.RevealingArea, eventCard!.Area.Type);
        Assert.Same(world.Seats[0].Hand, discarded.Area);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));

        if (reenter)
        {
            // A returning card is "a new copy of the card". The accepted
            // discard names its original incarnation across the suspension.
            int accepted = discarded.Incarnation;
            World.MoveToTop(discarded, world.AreaOf(
                DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(discarded, world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(discarded, world.Seats[0].Hand);
            Assert.NotEqual(accepted, discarded.Incarnation);
            Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Decline));
            Assert.Same(world.Seats[0].Hand, discarded.Area);
            Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
            return;
        }

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        Assert.Equal(DeckType.DiscardPile, discarded.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.False(world.Agenda.IsBusy);
    }
}
