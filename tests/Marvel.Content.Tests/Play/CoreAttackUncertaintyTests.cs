using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreAttackUncertaintyTests
{
    [Rule("rr:attack-enemy-activation.step.1")]
    [Rule("rr:boost-boost-icon.6")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryMinionDefenseMentionsBoostsOnlyWhenOneActuallyWaits(bool supplied)
    {
        // "If a minion without the villainous keyword is attacking, skip
        // this step." A boost supplied earlier stays until its activation.
        // This canonical Core state isolates defense after the deal step.
        var (_, world) = Playing(_ => { }, hero: true);
        Card minion = world.CreateCard(AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Attack = new EnemyAttack(minion.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        world.Activation = new EnemyActivation(minion.ObjectId, 0, true, Id: 1);
        if (supplied) Attack.GiveAdditionalBoostCard(world, minion, "test", []);
        string before = world.Digest().Canonical();
        Prompt prompt = Attack.DeclareDefender(world, world.Facts, new NoCardAbilities())!;
        Assert.Equal(before, world.Digest().Canonical());
        Assert.All(prompt.Affordances, option =>
        {
            Assert.Equal(supplied, option.Description!.Contains("Boosts", StringComparison.Ordinal));
            Assert.Contains("later effects", option.Description, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(supplied, prompt.Description!.Contains("boost icons", StringComparison.Ordinal));
    }

    [Fact]
    public void ARevealedBoostStillResolvingIsNotMistakenForAnEmptyBoostQueue()
    {
        // Synthetic suspended-boost state tests the generic description
        // contract; it does not claim this Core card has a suspending boost.
        var (_, world) = Playing(_ => { }, hero: true);
        Card minion = world.CreateCard(AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01180", world.AreaOf(DeckType.BoostingArea));
        world.Attack = new EnemyAttack(minion.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        world.Activation = new EnemyActivation(minion.ObjectId, 0, true, Id: 1);
        Prompt prompt = Attack.DeclareDefender(world, world.Facts, new NoCardAbilities())!;
        Assert.All(prompt.Affordances, option => Assert.Contains("Boosts", option.Description));
    }
}
