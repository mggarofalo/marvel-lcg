using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Increases imminent damage that already exceeds its target's remaining hit points.</summary>
/// <remarks>
/// Follow Through (16045): rr:excess-damage measures damage dealt beyond remaining
/// hit points, so increasing the excess deals that much more damage at rr:damage
/// step 1. Damage that no longer exceeds them has no excess to increase.
/// </remarks>
internal static class AbilityExcessDamageIncrease
{
    // Equivalent mutant: the interrupt is offered only while the damage exceeds the
    // remaining hit points and no admitted step-1 replacement lowers it first, so
    // loosening HasExcess, admitting the effect unconditionally, or increasing
    // unconditionally survives.
    internal static long Amount(AbilityEnemyDamageEffect.IncreaseExcessDamage increase, AbilityDamageAndThreatContext context) =>
        HasExcess(context.World, context.Occurrence) ? context.Incoming + increase.Amount : context.Incoming;

    internal static bool HasExcess(World world, Occurrence occurrence) =>
        occurrence.Damage is { IsComplete: false } damage && damage.CurrentAmount > Math.Max(0,
            DamagePlacement.Health(world, world.Facts, damage.Target) - damage.Target.Damage);
}
