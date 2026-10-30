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
    /// <summary>Whether the occurrence names the attacked enemy of the active player attack.</summary>
    public static bool CanInstall(World world, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        return PlayerAttackReflection.Current(world) is { } operation
            && occurrence.Target >= 0 && occurrence.Target == operation.Attack.Enemy;
    }

    /// <summary>Retains the prevention on the active player attack for this incarnation of its enemy.</summary>
    public static void Install(World world, Occurrence occurrence)
    {
        if (!CanInstall(world, occurrence))
            throw new RulesNotImplementedException("attack damage prevention requires the attacked enemy of a begun player attack");
        var enemy = world.Cards[occurrence.Target];
        PlayerAttackReflection.Current(world)!.Prevented = (enemy.ObjectId, enemy.Incarnation);
    }

    /// <summary>Whether this attack damage is prevented by the active player attack.</summary>
    internal static bool Prevents(World world, DamageRequest request) =>
        request.FromAttack && PlayerAttackReflection.Current(world)?.Prevented is { } prevented
        && prevented.Card == request.Target.ObjectId && prevented.Incarnation == request.Target.Incarnation;
}
