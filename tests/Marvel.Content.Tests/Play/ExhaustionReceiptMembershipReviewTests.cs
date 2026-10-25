using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ExhaustionReceiptMembershipReviewTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.1")]
    public void ReceiptSearchFindsAUsefulPhysicalSubsetAndRejectsTheOtherSubset()
    {
        // Step 1 requires that "the effect of that ability can change the game state."
        // Synthetic support: the paid ally cannot be the recipient of its healing.
        var runner = Runner("01006", "Action", """
            {"heal":{"card":{"last":{"query":"charactersYouControlExceptCost"}},"amount":1}}
            """, cost: """
            {"exhaustChosen":{"from":{"query":"alliesYouControl"},"count":1}}
            """);
        Card? source = null;
        Card? hurt = null;
        Card? healthy = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            hurt = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            hurt.TakeDamage(1);
            healthy = board.CreateCard("01020", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, action => action.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        var rejected = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(new Decision(offer.Id, [hurt!.ObjectId])));
        Assert.Contains("cannot initiate this action", rejected.Message);
        Assert.Equal(before, world.Digest().Canonical());
        game.Resolve(new Decision(offer.Id, [healthy!.ObjectId]));
        Assert.Equal(0, hurt!.Damage);
        Assert.True(hurt.Ready);
        Assert.False(healthy.Ready);
    }
}
