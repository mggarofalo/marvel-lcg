using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A lasting effect healing an identity each time it deals any amount of damage to an enemy.</summary>
/// <remarks>
/// Schadenfreude (16032). rr:lasting-effects: the effect lasts its stated duration
/// and is treated as a constant ability, so it resolves at each damage instance
/// rather than in a response window. Overkill damage counts as damage the
/// attacker dealt (ruling 4648617b8449406e).
/// </remarks>
public static class EnemyDamageHealing
{
    /// <summary>The effect kind; its spelling is an engine choice.</summary>
    public const string Kind = "healEachTimeYouDamageAnEnemy";

    internal static void Resolve(World world, Card identity, List<GameEvent> events)
    {
        foreach (var effect in world.Effects.Registered.Where(effect => effect.Source == EffectSource.LastingEffect
                     && effect.Kind == Kind && effect.Affects == identity.ObjectId))
            DamageRecovery.Heal(world, world.Facts, identity, effect.Amount, Steps.DamageDealt, "Heal", events);
    }
}
