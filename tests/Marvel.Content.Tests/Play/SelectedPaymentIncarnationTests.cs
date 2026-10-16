using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SelectedPaymentIncarnationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:initiating-abilities.step.5")]
    public void SelectedExhaustionRetainsItsAcceptedIncarnationAcrossResourcePayment(bool reenter, bool exhausted)
    {
        // A returning card is "a new copy of the card." The accepted cost names
        // the selected copy; a generator's suspended cost cannot rebind that choice.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"spend":"Y"},{"exhaustChosen":{
                  "from":{"query":"alliesYouControl"},"count":1}}]},
                "effect":{"draw":{"player":"you","count":1}}
              }]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"Y"}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        Card? generator = null;
        Card? selected = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            var allies = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            generator = board.CreateCard("01030", allies);
            generator.TakeDamage(2);
            selected = board.CreateCard("01020", allies);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ActionVerb && offer.AnchorId == source!.ObjectId);
        int[] paying = [generator!.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(action.Costs!), paying));
        game.Resolve(new Decision(action.Id, [selected!.ObjectId], paying, Allocations: allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.True(selected.Ready);
        int incarnation = selected.Incarnation;
        if (reenter)
        {
            var area = selected.Area;
            World.MoveToTop(selected, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(selected, area);
            Assert.NotEqual(incarnation, selected.Incarnation);
        }

        if (exhausted) selected.Exhaust();
        int handBefore = world.Seats[0].Hand.Cards.Count;
        if (reenter || exhausted)
        {
            // A departed or exhausted copy prevents "a required cost from being paid".
            // Step 5 says to "abort this process"; the post-arrow draw cannot run.
            Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Decline));
            Assert.Equal(handBefore, world.Seats[0].Hand.Cards.Count);
            Assert.Equal(!exhausted, selected.Ready);
            return;
        }
        game.Resolve(Decision.Decline);
        Assert.Equal(handBefore + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(selected.Ready);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
}
