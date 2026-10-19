using Marvel.Content.Tests.Cards;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CampaignTechAbilityTests
{
    [Theory]
    [InlineData("04155", 0, 1, true, true)]
    [InlineData("04155", 0, 6, false, false)]
    [InlineData("04156", 5, 6, false, true)]
    [InlineData("04156", 0, 6, false, false)]
    public void PrintedTechRewardPaysItsPermanentCostAndResolvesItsOwnEffect(
        string face, int cardsDrawn, int remainingDamage, bool ready, bool hero)
    {
        // Printed text: Adrenaline Stims readies and heals 5; Tactical Scanner draws 5.
        Card? reward = null;
        var (game, world) = Playing(board =>
        {
            reward = board.CreateCard(face, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            var setup = new CampaignGameSetup("synthetic", false, CampaignStage.Crossbones, 1,
                [new CampaignSeatSetup(0, [], null, false, [], false)], [], 0);
            new CampaignAttemptState(setup,
                [new CampaignCardBinding(reward.ObjectId, 0, new CampaignCard("tech:" + face, face, CampaignCardRole.Tech))]).Bind(board);
            board.Seats[0].IdentityCard.TakeDamage(6);
            board.Seats[0].IdentityCard.Exhaust();
        }, hero: hero, abilities: AuthoredCards.Runner());
        int held = world.Seats[0].Hand.Cards.Count;
        if (!hero)
        {
            Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == reward!.ObjectId);
            Assert.False(Assert.Single(world.Campaign!.Cards).Removed);
            Assert.Equal(6, world.Seats[0].IdentityCard.Damage);
            return;
        }
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == reward!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(held + cardsDrawn, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(remainingDamage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(ready, world.Seats[0].IdentityCard.Ready);
        Assert.Equal(DeckType.DiscardPile, reward!.Area.Type);
        Assert.True(Assert.Single(world.Campaign!.Cards).Removed);
    }
}
