using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Read-only eligibility for interrupts to the damage still imminent in a window.</summary>
public static class DamageWindowEligibility
{
    /// <summary>Whether the window's recipient still has damage available to prevent.</summary>
    public static bool HasRemainingDamage(World world, Occurrence occurrence)
    {
        if (occurrence.Damage is not { } damage) return true;
        long remaining = damage.CurrentAmount;
        // rr:prevent.1: "reduce the amount of damage the target takes".
        // faq:05017: "After you resolve the first one, there is no more damage
        // left to trigger the other ones." Reading prevention does not spend it
        // or rewrite the dealt amount retained by the damage procedure.
        foreach (var effect in world.Effects.Active())
            if (effect.Kind == "preventDamage" && effect.Affects == damage.Target.ObjectId)
                remaining = Math.Max(0, remaining - (effect.Amount <= 0 ? remaining : effect.Amount));
        return remaining > 0;
    }
}
