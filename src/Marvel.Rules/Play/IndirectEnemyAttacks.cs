using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>An enemy whose own text makes each of its attacks deal indirect damage.</summary>
/// <remarks>
/// Printed 16137 Starshark: "Starshark's attacks deal indirect damage."
/// <c>rr:indirect-damage.5</c>: "If an enemy's attack deals indirect damage, the
/// indirect damage is dealt during step four of the enemy activation (after
/// player's have the opportunity to defend against the attack)." The attack is
/// marked indirect as it is prepared, the same mark a boost ability gives it.
/// The modifier spelling is an engine choice.
/// </remarks>
public static class IndirectEnemyAttacks
{
    /// <summary>Engine-selected modifier for "this enemy's attacks deal indirect damage".</summary>
    public const string Field = "attacks_deal_indirect_damage";

    /// <summary>Whether this enemy's attacks deal indirect damage.</summary>
    public static bool Deal(World world, Card enemy)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(enemy);
        return StateFields.Modified(world, enemy, Field, world.Facts, world.Players) > 0;
    }
}
