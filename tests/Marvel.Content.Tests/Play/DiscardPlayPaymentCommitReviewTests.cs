using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic payment controls for the shared hand/discard play boundary.</summary>
public sealed class DiscardPlayPaymentCommitReviewTests
{
    [Fact]
    [Rule("rr:ally-limit")]
    public void AuthoredNickFuryAtAllyLimitOffersOneChoiceAndDrawsThreeOnce()
    {
        // The mandatory discard "occurs before abilities that resolve upon entering play."
        // Printed 01084: "draw 3 cards" is one of his forced entry response's choices.
        Card nick = null!;
        Card chosen = null!;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            nick = board.CreateCard("01084", board.Seats[0].Hand);
            var area = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            chosen = board.CreateCard("01068", area);
            board.CreateCard("01070", area);
            board.CreateCard("01083", area);
            paying = Enumerable.Range(0, 4).Select(_ => board.CreateCard(
                "01093", board.Seats[0].Hand).ObjectId).ToArray();
        });
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == nick.ObjectId);
        int afterPayment = world.Seats[0].Hand.Cards.Count - 5;
        game.Resolve(PaidDecision.Take(offer, paying) with
        { Targets = [world.Seats[0].IdentityCard.ObjectId] });
        Assert.Equal(afterPayment, world.Seats[0].Hand.Cards.Count);
        Assert.False(game.IsRootPrompt);
        var discard = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == chosen.ObjectId);
        game.Resolve(Decision.Take(discard.Id));
        Assert.Equal(DeckType.DiscardPile, chosen.Area.Type);
        Assert.Equal(afterPayment, world.Seats[0].Hand.Cards.Count);
        var draw = Assert.Single(game.Pending!.Affordances, item => item.Description == "Draw 3 cards");
        game.Resolve(Decision.Take(draw.Id));
        Assert.Equal(afterPayment + 3, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(3, nick.Area.Cards.Count);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    public void AcceptedDiscountedPlayCommitsGeneratorAndHandResourcesOnce(bool discardOrigin)
    {
        // "Pay the cost(s)." Preparation must not execute this synthetic resource
        // ability or consume the one-use discount before the accepted payment.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05018","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"allowPlayFromDiscard":"this"}
            }]},{"card":"01030","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
              "cost":{"seq":[{"exhaust":"this"},{"dealDamage":{"cards":"this","amount":1}}]},
              "effect":{"generate":"G"}
            }]}]}
            """));
        Card ally = null!;
        Card generator = null!;
        int[] handResources = [];
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("05018", discardOrigin
                ? board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0)
                : board.Seats[0].Hand);
            generator = board.CreateCard("01030", board.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            handResources = Enumerable.Range(0, 3).Select(_ => board.CreateCard(
                "01093", board.Seats[0].Hand).ObjectId).ToArray();
            CardPayment.ReduceNextCardCost(board, generator, 0, 1);
        }, abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally.ObjectId);
        Assert.Equal("3", Assert.Single(offer.Costs!).Cost);
        Assert.True(generator.Ready);
        Assert.Equal(0, generator.Damage);
        int[] paying = [handResources[0], generator.ObjectId, handResources[1]];

        game.Resolve(PaidDecision.Take(offer, paying) with
        { Targets = [world.Seats[0].IdentityCard.ObjectId] });

        Assert.False(generator.Ready);
        Assert.Equal(1, generator.Damage);
        Assert.All(handResources.Take(2), id => Assert.Equal(
            DeckType.DiscardPile, world.Cards[id].Area.Type));
        Assert.Same(world.Seats[0].Hand, world.Cards[handResources[2]].Area);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[0], ally).Amount);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
