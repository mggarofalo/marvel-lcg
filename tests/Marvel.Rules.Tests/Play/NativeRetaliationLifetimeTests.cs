using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class NativeRetaliationLifetimeTests : AttackTestBase
{
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:attack-enemy-activation.step.6")]
    [Rule("rr:retaliate-x.1")]
    [Theory]
    [InlineData(TimingPoints.EndOfAttack, 0, 1)]
    [InlineData(TimingPoints.EndOfPhase, 2, 1)]
    [InlineData(TimingPoints.EndOfPhase, 2, 0)]
    [InlineData(null, 2, 1)]
    public void CompletionReadsLiveKeywordAfterExplicitDurationExpiry(string? until, int damage, int attack)
    {
        // rr:lasting-effects.5: "A lasting effect expires as soon as the timing
        // point specified by its duration is reached." rr:retaliate-x.1:
        // "After this character is attacked, deal X damage to the attacker."
        // This synthetic grant says UNTIL end of attack, not "for this attack".
        var facts = Printed(atk: attack, boost: 0);
        var world = Board(facts);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "retaliate", 2, Affects: hero.ObjectId, Lasts: new Duration(Until: until)));

        Finish(world, facts);

        Assert.Equal(damage, villain.Damage);
        Assert.Equal(attack, hero.Damage);
        Assert.Equal(until == TimingPoints.EndOfAttack ? 0 : 2,
            StateFields.Modified(world, hero, "retaliate", facts, 1));
    }
}
