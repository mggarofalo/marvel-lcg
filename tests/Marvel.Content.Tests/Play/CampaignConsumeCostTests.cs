using Marvel.Content.Tests.Cards;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CampaignConsumeCostTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ConsumingRewardDiscardsTheBoundPhysicalCardAndRecordsRemovalBeforeTheEffect()
    {
        // rr:initiating-abilities.step.5: costs are paid before the ability's effects initiate.
        Card? reward = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"consumeCampaignCard":"this"}""");
        var (game, world) = Playing(board =>
        {
            reward = InPlay(board, AuthoredCards.AuntMay);
            Bind(board, reward, CampaignCardRole.Tech);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var before = world.Digest().Canonical();
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == reward!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.DiscardPile, reward!.Area.Type);
        Assert.True(Assert.Single(world.Campaign!.Cards).Removed);
        Assert.False(world.Campaign.CanRemove(reward));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.NotEqual(before, world.Digest().Canonical());
        World.MoveToTop(reward, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.False(world.Campaign.CanRemove(reward));
    }

    [Theory]
    [InlineData("untracked")]
    [InlineData("condition")]
    [InlineData("removed")]
    [Rule("rr:initiating-abilities.step.3")]
    public void UnavailableCampaignInstanceDoesNotOfferItsAbility(string state)
    {
        // rr:initiating-abilities.step.3: check the player's ability to pay the costs.
        Card? reward = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""", cost: """{"consumeCampaignCard":"this"}""");
        var (game, world) = Playing(board =>
        {
            reward = InPlay(board, AuthoredCards.AuntMay);
            if (state == "untracked") return;
            Bind(board, reward, state == "condition" ? CampaignCardRole.Condition : CampaignCardRole.Tech);
            if (state == "removed") board.Campaign!.Remove(reward, "synthetic", []);
        }, abilities: runner);

        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == reward!.ObjectId);
        Assert.Equal(DeckType.SupportsArea, reward!.Area.Type);
        Assert.Equal(6, world.Seats[0].Hand.Cards.Count);
    }

    private static void Bind(World world, Card card, CampaignCardRole role)
    {
        var setup = new CampaignGameSetup("synthetic", false, CampaignStage.Crossbones, 1,
            [new CampaignSeatSetup(0, [], null, false, [], false)], [], 0);
        new CampaignAttemptState(setup,
            [new CampaignCardBinding(card.ObjectId, 0, new CampaignCard("reward:one", card.FaceId, role))]).Bind(world);
    }
}
