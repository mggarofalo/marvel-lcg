using System.Globalization;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Bounds the total damage a character takes from a single attack.</summary>
/// <remarks>
/// Printed 16094 Cutthroat Ambition: "Nebula cannot take more than 5 damage from a
/// single attack." rr:cannot makes the bound absolute, so it applies to the damage the
/// character would take after every reduction and prevention, across every damage
/// instance of the attack, overkill included. Several maxima on one character are each
/// a prohibition, so the smallest applies. The modifier and ledger spellings are
/// engine choices.
/// </remarks>
public static class AttackDamageMaximum
{
    /// <summary>Engine-selected modifier for "cannot take more than X damage from a single attack".</summary>
    public const string Field = "attack_damage_maximum";

    private const string Taken = "AttackDamageTaken";

    /// <summary>The smallest live maximum on this character, or null when none applies.</summary>
    public static long? Amount(World world, Card target)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        long? maximum = null;
        foreach (var effect in world.Effects.Active())
        {
            if (effect.ValueRole != ContinuousValueRole.Modifier
                || !string.Equals(effect.Kind, Field, StringComparison.Ordinal)
                || !effect.AppliesTo(world, target)) continue;
            long amount = Math.Max(0, effect.LiveAmount?.Evaluate(world) ?? effect.Amount);
            maximum = maximum is { } smaller ? Math.Min(smaller, amount) : amount;
        }
        return maximum;
    }

    /// <summary>The damage this character can still take from the current attack, or null when unbounded.</summary>
    public static long? Remaining(World world, Card target)
    {
        if (Amount(world, target) is not { } maximum) return null;
        return AttackDamageReduction.CurrentScope(world) is { } scope ? Math.Max(0, maximum - TakenSoFar(world, Key(scope, target))) : maximum;
    }

    /// <summary>The damage taken after this attack's maximum, recording what the attack has dealt.</summary>
    internal static long Apply(World world, Card target, long taken)
    {
        if (taken <= 0 || Amount(world, target) is not { } maximum) return taken;
        string key = Key(AttackDamageReduction.CurrentScope(world)
            ?? throw new RulesNotImplementedException("an attack damage maximum requires an active attack"), target);
        long bounded = Math.Min(taken, Math.Max(0, maximum - TakenSoFar(world, key)));
        if (bounded > 0)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, key, bounded,
                Card: target.ObjectId, Affects: target.ObjectId,
                Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));
        return bounded;
    }

    private static long TakenSoFar(World world, string key) => world.Effects.Active()
        .Where(effect => string.Equals(effect.Kind, key, StringComparison.Ordinal)).Sum(effect => effect.Amount);

    private static string Key(string scope, Card target) =>
        $"{Taken}:{scope}:{target.ObjectId.ToString(CultureInfo.InvariantCulture)}:"
        + target.Incarnation.ToString(CultureInfo.InvariantCulture);
}
