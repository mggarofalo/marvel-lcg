using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ActiveAttackProjectionTests
{
    [Fact]
    public void PublicAttackRemainsVisibleWithoutPermissionToSeeTheDecision()
    {
        var world = VisibilityFixture.Board();
        int source = world.Cards.Single(card => card.FaceId == "public-villain").ObjectId;
        int target = world.Cards.Single(card => card.FaceId == "player-ally").ObjectId;
        world.Attack = new EnemyAttack(source, 1, target);

        var attack = Assert.IsType<ActiveAttackDescriptor>(
            WorldProjection.For(world, null, [], ViewScope.None).World.ActiveAttack);

        Assert.Equal(new ActivitySubject(source, "public-villain", ActivitySubjectRole.Source), attack.Source);
        Assert.Equal(new ActivitySubject(target, "player-ally", ActivitySubjectRole.Target), attack.Target);
        world.Attack = null;
        world.FinishedAttack = new EnemyAttack(source, 1, target);
        Assert.Null(WorldProjection.For(world, null, [], ViewScope.None).World.ActiveAttack);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SyntheticConcealedParticipantCannotLeakThroughAttackContext(bool hiddenSource)
    {
        // Synthetic boundary contract, not a claim that a card in hand can attack.
        var world = VisibilityFixture.Board();
        int visible = world.Cards.Single(card => card.FaceId == "public-villain").ObjectId;
        int concealed = world.Cards.Single(card => card.FaceId == "one-hand").ObjectId;
        world.Attack = new EnemyAttack(hiddenSource ? concealed : visible, 1,
            hiddenSource ? visible : concealed);
        Assert.Null(WorldProjection.For(world, null, [], ViewScope.None).World.ActiveAttack);
        var authorized = new RestrictedVisibilityPolicy(1).Authorize(null, world.Players);
        Assert.NotNull(WorldProjection.For(world, null, [], authorized).World.ActiveAttack);
    }
}
