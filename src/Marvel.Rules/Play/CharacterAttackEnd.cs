using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Ends lasting and delayed effects bounded by a hero's or ally's attack.</summary>
/// <remarks>
/// rr:delayed-effect.1: delayed effects "resolve automatically and immediately
/// after their specified timing point". Last Stand (15029) bounds its ATK bonus
/// by "that attack" and delays its discard until "that attack resolves".
/// Engine choice: a player attack made while an enemy attack is in progress
/// leaves attack-bounded effects to that enemy attack's own end, because both
/// share one duration; <see cref="Bounded"/> lets an ability refuse that case.
/// </remarks>
public static class CharacterAttackEnd
{
    /// <summary>Whether a player attack's end can end effects bounded by it.</summary>
    public static bool Bounded(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.Attack is null;
    }

    internal static void Complete(World world, Occurrence occurrence, List<GameEvent> events)
    {
        occurrence.Also(Steps.AttackEnds);
        if (!Bounded(world)) return;
        world.Effects.Expire(TimingPoints.EndOfAttack, events);
        DelayedEffects.Occur(world, Steps.AttackEnds, -1, events);
    }
}
