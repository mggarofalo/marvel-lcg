using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Prevents all damage one begun player attack would deal to its attacked enemy.</summary>
/// <remarks>
/// <para>
/// Temporal Shield (11014): "When Kang is attacked, discard Temporal Shield →
/// prevent all damage from this attack". Ruling 310af249be272877: it "only
/// prevents the damage that would be dealt to Kang during an attack", not the
/// damage the same attack deals to other enemies.
/// </para>
/// <para>
/// Ruling a806907d1743e0d1: "Temporal Shield resolves entirely before the tough
/// status card, meaning the tough status card will have no damage to prevent and
/// will not be discarded." The prevention therefore fixes the damage dealt at
/// rr:damage.step.1, before Tough is considered, rather than reducing damage
/// taken afterwards.
/// </para>
/// </remarks>
public static class PlayerAttackPrevention
{
    /// <summary>Whether <paramref name="occurrence"/> initiates a player attack against a known <paramref name="enemy"/>.</summary>
    /// <remarks>
    /// rr:attack-player-ability-type.5: an attack against several enemies "is
    /// considered to have attacked each of those enemies", so the prevented enemy
    /// is the attacked enemy the triggering ability names, not only the attack's
    /// first target. The trigger's own match decides which recipient that is.
    /// </remarks>
    public static bool CanInstall(World world, Occurrence occurrence, Card? enemy)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        return world.Agenda.PlayerAttackOperation(occurrence) is not null
            && enemy is not null;
    }

    /// <summary>Retains the prevention on that player attack for this incarnation of <paramref name="enemy"/>.</summary>
    public static void Install(World world, Occurrence occurrence, Card? enemy)
    {
        if (!CanInstall(world, occurrence, enemy))
            throw new RulesNotImplementedException("attack damage prevention requires an attacked enemy of a begun player attack");
        world.Agenda.PlayerAttackOperation(occurrence)!.Prevented = (enemy!.ObjectId, enemy.Incarnation);
    }

    /// <summary>Whether this attack damage is prevented by the active player attack.</summary>
    /// <remarks>
    /// The incarnation keeps the prevention to the enemy that was attacked. An
    /// attack on an enemy that leaves play deals it nothing more, so no current
    /// card reaches a re-entered incarnation within the same attack.
    /// </remarks>
    internal static bool Prevents(World world, DamageRequest request) =>
        request.FromAttack && PlayerAttackReflection.Current(world)?.Prevented is { } prevented
        && prevented.Card == request.Target.ObjectId && prevented.Incarnation == request.Target.Incarnation;
}
