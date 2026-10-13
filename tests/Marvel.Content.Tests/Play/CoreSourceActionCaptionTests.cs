using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreSourceActionCaptionTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData("01038", "Attack: 1 damage (2 with AERIAL)")]
    [InlineData("01039", "Gain AERIAL until end of player phase")]
    public void TechActionsExposeKnownEffectWithoutChangingSourceOrCommandIdentity(string face, string caption)
    {
        var world = Deal("iron_man");
        world.Seats[0].IdentityCard.TurnTo("01029a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Card source = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01089", world.Seats[0].Hand);
        var ability = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        string before = world.Digest().Canonical();
        var offer = runner.Describe(world, ability);
        Assert.Equal(caption, offer.DisplayLabel);
        Assert.Equal(caption, offer.CommitLabel);
        Assert.Equal(source.ObjectId, offer.AnchorId);
        Assert.Equal(Cards.Title(face), offer.Label);
        Assert.Equal(AbilityType.Action, offer.Timing);
        Assert.Contains($"Exhaust {Cards.Title(face)}", offer.CostDescription);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(source.Ready);
    }
}
