using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual cards on a prepared board; this does not admit the Ant-Man pack.
public sealed class TeamBuildingExerciseIndependentReviewTests
{
    [Theory]
    [InlineData("01072", 1)]
    [InlineData("01088", 2)]
    [Rule("rr:cost.4.1")]
    [Rule("rr:ally-limit")]
    public void DiscountDoesNotCountAsAnOverpaidResource(string resourceFace, long expected)
    {
        // Excess resources "were not paid for that cost". Ruling 5e6ac90dc4187eef
        // says a wholly overpaid Power Of card generates one resource for Hank.
        Card? source = null, hank = null, resource = null, old = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "12024");
            hank = board.CreateCard("12011", board.Seats[0].Hand);
            resource = board.CreateCard(resourceFace, board.Seats[0].Hand);
            var area = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            old = board.CreateCard("01067", area);
            board.CreateCard("01068", area);
            board.CreateCard("01041", area);
        }, hero: true, abilities: AuthoredCards.Runner());
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var occurrence = world.Agenda.Occurrence!;
        var parent = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        Assert.False(source.Ready);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == hank!.ObjectId);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal("0", price.Cost);
        int[] payment = [resource!.ObjectId];
        game.Resolve(new Decision(offer.Id, [], payment, Allocations: ResourcePayment.Allocate(price, payment)));
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Equal(DeckType.RevealingArea, hank!.Area.Type);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(parent));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == hank.ObjectId).Id));
        Assert.Equal(expected, hank.Tokens.GetValueOrDefault("c_pym"));
        Assert.Equal(expected, DamagePlacement.Health(world, world.Facts, hank));
        Assert.Equal(Steps.ChooseAllyForLimit, game.Pending!.Trigger);
        game.Resolve(Decision.Take(old!.ObjectId));
        Assert.Equal(DeckType.DiscardPile, old.Area.Type);
        Assert.Equal(DeckType.AlliesArea, hank.Area.Type);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(parent));
        Assert.False(world.Agenda.IsBusy);
    }
}
