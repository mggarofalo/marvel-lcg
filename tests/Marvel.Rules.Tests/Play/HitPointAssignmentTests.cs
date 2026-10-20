using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class HitPointAssignmentTests : BasicPowerTestBase
{
    [Theory]
    [InlineData(-2, 8)]
    [InlineData(3, 13)]
    [Rule("rr:maximum-hit-points")]
    public void AssignmentUsesTheLiveModifiedMaximum(int modifier, int expectedDamage)
    {
        // Maximum HP is "their base hit points plus or minus all “gets” hit point modifiers that are active on that character."
        var world = Board(new Printed().With("hero", ("HP", "11")));
        var identity = world.Seats[0].IdentityCard;
        world.Effects.Register(new(EffectSource.LastingEffect, "health", modifier, Affects: identity.ObjectId));
        identity.TakeDamage(20);
        Assert.True(HitPointAssignment.Set(world, identity, 1, "Test", []));
        Assert.Equal(expectedDamage, identity.Damage);
        Assert.Equal(1, DamagePlacement.Health(world, world.Facts, identity) - identity.Damage);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(20)]
    [Rule("rr:hit-points.2")]
    public void SettingOneRemainingHitPointRemovesAllExcessLethalDamage(int damage)
    {
        // "An identity’s or villain’s hit point dial represents their remaining hit points."
        // Synthetic arithmetic control for printed 03008: "set his hit point dial to 1 instead."
        var world = Board(new Printed().With("hero", ("HP", "11")));
        var identity = world.Seats[0].IdentityCard;
        identity.TakeDamage(damage);
        var events = new List<GameEvent>();
        Assert.True(HitPointAssignment.Set(world, identity, 1, "Test", events));
        Assert.Equal(10, identity.Damage);
        var change = Assert.Single(events.OfType<FieldSet>());
        Assert.Equal(0, change.From);
        Assert.Equal(1, change.To);
        Assert.Equal("health", change.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12)]
    public void UnsupportedAssignmentsFailBeforeChangingState(int remaining)
    {
        // This bounded primitive does not invent defeat timing for zero or above-maximum health.
        var world = Board(new Printed().With("hero", ("HP", "11")));
        var identity = world.Seats[0].IdentityCard;
        identity.TakeDamage(11);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() => HitPointAssignment.Set(world, identity, remaining, "Test", events));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Empty(events);
    }
}
