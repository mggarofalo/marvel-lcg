using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>One cost target's projected damage and consumable prevention.</summary>
internal sealed class AbilityHealingTarget(World world, Card target)
{
    internal long Damage { get; set; } = target.Damage;
    private long tough = Statuses.Count(world, target, Statuses.Tough);
    private readonly List<(ContinuousEffect Effect, int? Remaining)> prevention =
        [.. DamagePrevention.CaptureUses(world, target)];

    internal long TakeDamage(long amount)
    {
        var assignment = DamageAssignment.AfterReplacement(amount, tough > 0);
        if (assignment.Dealt <= 0) return 0;
        if (assignment.SpendsTough) tough--;
        long taken = Prevent(assignment.Taken);
        Damage = AbilityAmounts.SaturatingSum(Damage, [taken]);
        return taken;
    }

    private long Prevent(long amount)
    {
        // Live Tough prevention also consumes every active prevention registration.
        foreach (var effect in prevention.Select(entry => entry.Effect).ToArray())
        {
            int index = prevention.FindIndex(entry => entry.Effect == effect && entry.Remaining != 0);
            if (index < 0) continue;
            var entry = prevention[index];
            if (entry.Remaining is int remaining) prevention[index] = (entry.Effect, remaining - 1);
            amount = DamagePrevention.After(amount, effect.Amount);
        }
        return amount;
    }
}
