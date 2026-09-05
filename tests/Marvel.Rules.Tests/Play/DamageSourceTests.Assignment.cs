using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed partial class DamageSourceTests
{
    [Rule("rr:tough.2")]
    [Rule("rr:tough.2.1")]
    [Rule("rr:tough.2.2")]
    [Rule("rr:tough.3")]
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 2, 0, 2)]
    [InlineData(1, 0, 1, 0)]
    [InlineData(1, 2, 0, 1)]
    [InlineData(4, 1, 0, 0)]
    public void AssignmentAndLiveDamageSpendOneToughOnlyForPositiveDamage(
        long amount, int tough, long taken, int remainingTough)
    {
        // "Prevent all of that damage and discard a tough status card";
        // "only one tough status card each time"; at zero "does not lose
        // their tough status card". Prevention means no damage was taken.
        var facts = new Printed();
        var world = Board(facts);
        var target = world.CreateCard("target", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        for (int index = 0; index < tough; index++)
        {
            Statuses.Give(world, target, Statuses.Tough);
        }
        string before = world.Digest().Canonical();

        var assignment = DamageAssignment.AfterReplacement(
            amount, Statuses.Has(world, target, Statuses.Tough));

        Assert.Equal(amount, assignment.Dealt);
        Assert.Equal(taken, assignment.Taken);
        Assert.Equal(tough - remainingTough == 1, assignment.SpendsTough);
        Assert.Equal(before, world.Digest().Canonical());
        var events = new List<GameEvent>();

        Assert.False(Damage.Deal(
            world, facts, world.Seats[0].IdentityCard, target, amount, "test", "Damage", events));

        Assert.Equal(taken, target.Damage);
        Assert.Equal(remainingTough, Statuses.Count(world, target, Statuses.Tough));
        Assert.Equal(taken > 0 ? 1 : 0,
            events.OfType<FieldSet>().Count(change => change.Field == "health"));
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:damage.step.2")]
    [Rule("rr:damage.step.3")]
    [Rule("rr:damage.3.2")]
    [Theory]
    [InlineData(0, false, 4, 0, false)]
    [InlineData(0, true, 4, 0, false)]
    [InlineData(3, true, 4, 0, false)]
    [InlineData(3, false, 1, 1, true)]
    [InlineData(3, false, 0, 0, true)]
    [InlineData(3, false, -1, 0, true)]
    public void AssignmentAndLivePreventionPreserveDealtDamageAndTiming(
        long replaced, bool tough, long preventedAmount, long taken, bool visitsStepThree)
    {
        // Step 1 fixes dealt damage, tough is step 2, and taking modifiers
        // are step 3: "the amount of damage dealt is not modified" (.3.2).
        // Damage completely replaced or prevented by tough never reaches step 3.
        var facts = new Printed();
        var world = Board(facts);
        var target = world.CreateCard("target", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        if (tough)
        {
            Statuses.Give(world, target, Statuses.Tough);
        }
        var abilities = new AssignmentModifiers(replaced, preventedAmount);
        world.Abilities = abilities;

        var assignment = DamageAssignment.AfterReplacement(replaced, tough)
            .AfterPrevention(preventedAmount);

        Assert.Equal(replaced, assignment.Dealt);
        Assert.Equal(taken, assignment.Taken);
        Assert.Equal(tough && replaced > 0, assignment.SpendsTough);
        Assert.False(abilities.VisitedStepThree);

        Damage.Deal(world, facts, world.Seats[0].IdentityCard,
            target, 3, "test", "Damage", []);

        Assert.Equal(taken, target.Damage);
        Assert.Equal(visitsStepThree, abilities.VisitedStepThree);
        Assert.Equal(tough && replaced == 0, Statuses.Has(world, target, Statuses.Tough));
    }

    private sealed class AssignmentModifiers(long replaced, long taken) : NoCardAbilities
    {
        public bool VisitedStepThree { get; private set; }

        public override long WouldBeDealt(
            World world, Card target, Card source, long amount, List<GameEvent> events) => replaced;

        public override long WouldTake(
            World world, Card target, Card source, long amount, List<GameEvent> events)
        {
            VisitedStepThree = true;
            return taken;
        }
    }
}
