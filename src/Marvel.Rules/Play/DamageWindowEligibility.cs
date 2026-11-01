using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Read-only eligibility for interrupts to the damage still imminent in a window.</summary>
public static class DamageWindowEligibility
{
    /// <summary>Whether the window's recipient still has damage available to prevent.</summary>
    public static bool HasRemainingDamage(World world, Occurrence occurrence) =>
        occurrence.DirectDamage is not null || occurrence.Damage is not null
            ? RemainingDamage(world, occurrence) > 0
            : true;

    /// <summary>The damage the window's recipient would still take after pending prevention.</summary>
    public static long RemainingDamage(World world, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (occurrence.DirectDamage is { } direct) return direct.Remaining;
        if (occurrence.Damage is not { } damage)
            throw new RulesNotImplementedException("no damage is imminent in this window");
        // rr:damage orders tough status cards (step 2) before "would take" abilities
        // (step 3); rr:tough.2 prevents all of the damage. Ruling 7cf2a0ea8a6b2a1f: the
        // tough status card prevents the damage before Groot's Flora Colossus triggers.
        // Equivalent mutant: only Lightning Strike (06006) ignores Tough, and it damages
        // enemies, none of which prints a "would take" interrupt.
        if (!damage.IgnoresTough && Statuses.Has(world, damage.Target, Statuses.Tough)) return 0;
        long remaining = damage.CurrentAmount;
        // rr:prevent.1: "reduce the amount of damage the target takes".
        // faq:05017: "After you resolve the first one, there is no more damage
        // left to trigger the other ones." Reading prevention does not spend it
        // or rewrite the dealt amount retained by the damage procedure.
        foreach (var effect in DamagePrevention.Active(world, damage.Target))
            remaining = DamagePrevention.After(remaining, effect.Amount);
        return remaining;
    }
}
