using System.Globalization;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Reduces the damage a character takes from each attack, once per attack.</summary>
/// <remarks>
/// Printed 14027 Vibration Resistance: "Reduce the damage attached enemy takes
/// from each attack by 1." Ruling 3dfec18909dc931e: constant abilities reducing
/// damage apply before interrupt abilities that reduce damage taken. Tough has
/// priority (ruling c8268c2138f6c809), and the damage dealt is unchanged
/// (rr:damage.3.2). The modifier and ledger spellings are engine choices.
/// </remarks>
public static class AttackDamageReduction
{
    /// <summary>Engine-selected modifier for "takes X less damage from each attack".</summary>
    public const string Field = "attack_damage_reduction";

    private const string Spent = "AttackDamageReduced";

    /// <summary>The live reduction on this character.</summary>
    public static long Amount(World world, Card target)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        return Math.Max(0, StateFields.Modified(world, target, Field, world.Facts, world.Players));
    }

    /// <summary>The damage taken after this attack's reduction, recording that the attack used it.</summary>
    internal static long Apply(World world, Card target, long taken)
    {
        long reduction = Amount(world, target);
        if (reduction == 0 || taken <= 0) return taken;
        string key = $"{Spent}:{Scope(world)}:{target.ObjectId.ToString(CultureInfo.InvariantCulture)}:"
            + target.Incarnation.ToString(CultureInfo.InvariantCulture);
        // "From each attack": a second damage instance of the same attack to the
        // same character is not reduced again.
        if (world.Effects.Active().Any(effect => string.Equals(effect.Kind, key, StringComparison.Ordinal)))
            return taken;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, key,
            Card: target.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));
        return Math.Max(0, taken - reduction);
    }

    private static string Scope(World world) => world.Agenda.PlayerAttackScope() is { } frame
        ? "player:" + frame.ToString(CultureInfo.InvariantCulture)
        : world.Activation is { Attacking: true } activation
            ? "enemy:" + activation.Id.ToString(CultureInfo.InvariantCulture)
            : throw new RulesNotImplementedException("an attack damage reduction requires an active attack");
}
