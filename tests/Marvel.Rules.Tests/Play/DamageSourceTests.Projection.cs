using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed partial class DamageSourceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UncertainDamagePreviewDoesNotChooseAnAmountOrSpendAStatus(bool unsupported)
    {
        // The engine chooses these result shapes. Missing projection support
        // is not a rules prohibition, and possible amounts are not decisions.
        var facts = new Printed();
        var world = Board(facts);
        var target = world.CreateCard("target", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, target, Statuses.Tough);
        RuleProjection<long> result = unsupported
            ? new RuleProjection<long>.Unsupported("test replacement projection is unsupported")
            : new RuleProjection<long>.Possible([1, 3]);
        world.Abilities = new ProjectedDamage(result);
        string before = world.Digest().Canonical();

        string preview = Damage.PreviewDamage(
            world, facts, world.Seats[0].IdentityCard, target, 3);

        Assert.Equal(unsupported
            ? "5/5 HP · test replacement projection is unsupported"
            : "5/5 HP · damage has multiple possible outcomes", preview);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(Statuses.Has(world, target, Statuses.Tough));

        // Live execution still follows the rule; it does not consume the
        // preview result as an amount or use unsupported as an illegality flag.
        Assert.False(Damage.Deal(world, facts, world.Seats[0].IdentityCard,
            target, 3, "test", "Damage", []));
        Assert.Equal(0, target.Damage);
        Assert.False(Statuses.Has(world, target, Statuses.Tough));
    }

    [Fact]
    public void PossibleProjectionCannotStandForAnEmptySetOfLegalAnswers()
    {
        Assert.Throws<ArgumentException>(() =>
            new RuleProjection<long>.Possible(ImmutableArray<long>.Empty));
        Assert.Throws<ArgumentException>(() =>
            new RuleProjection<long>.Possible(default));
    }

    private sealed class ProjectedDamage(RuleProjection<long> result) : NoCardAbilities
    {
        public override DamageProjection PreviewDamageReplacement(
            World world, Card target, Card source, long amount) => new(result);
    }
}
