using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HandResourceCommitmentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resource.1")]
    [Rule("rr:leaves-play.1")]
    public void HandContributionsRetainTheirCommitmentAcrossGeneratorSuspension(bool generatorFirst)
    {
        // A player generates resources "by discarding cards from their hand".
        // Synthetic resource instructions create an observable later generator cost.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"spend":"YYY"},"effect":{"draw":{"player":"you","count":1}}
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
        Card? handCard = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            handCard = board.CreateCard("01088", board.Seats[0].Hand);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ActionVerb && offer.AnchorId == source!.ObjectId);
        int[] paying = generatorFirst
            ? [generator!.ObjectId, handCard!.ObjectId]
            : [handCard!.ObjectId, generator!.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(action.Costs!), paying));
        game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        if (generatorFirst)
        {
            Assert.Equal(DeckType.HandsArea, handCard.Area.Type);
            // rr:leaves-play.1: a returned card is "a new copy of that card".
            // Crossing play makes the accepted contributor a different copy.
            World.MoveToTop(handCard, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(handCard, world.Seats[0].Hand);
            var error = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Decline));
            Assert.Contains("accepted copy and contribution", error.Message, StringComparison.Ordinal);
            Assert.Equal(DeckType.HandsArea, handCard.Area.Type);
            return;
        }
        Assert.Equal(DeckType.DiscardPile, handCard.Area.Type);
        // A later effect may return a paid card. Payment holds no deferred discard of it.
        World.MoveToTop(handCard, world.Seats[0].Hand);
        game.Resolve(Decision.Decline);

        Assert.Equal(DeckType.HandsArea, handCard.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
