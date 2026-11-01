using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Totals the damage one attack deals to each character, for "with a single attack" abilities.</summary>
/// <remarks>
/// Power Stone (16149) answers a hero or villain dealing 3 or more damage to its host "with a
/// single attack". The damage is dealt, not taken (ruling ea69fd70a070ee2c), and overkill damage
/// is damage from the attack (ruling b6fbf7538dd6b8d2; rr:overkill.2). A player attack is
/// answered after the attack's ability or basic power; an enemy attack in its shared step-six
/// window (rr:attack-enemy-activation.step.6).
/// </remarks>
public static class AttackDealtDamageTiming
{
    /// <summary>An attack dealt damage. The condition name is an engine choice.</summary>
    public const string AttackDealtDamage = "WhenAttackDealtDamage";

    internal static void Record(World world, OccurrenceCard attacker, Damage.PlacedDamage damage, Occurrence occurrence)
    {
        if (damage.Dealt <= 0) return;
        // Equivalent mutant: capturing no damage taken. Power Stone reads damage dealt only.
        var recipient = Attacked.Capture(damage.Target, world.Facts, damage.Taken);
        // An enemy attack's spill carries no attack record of its own, so the live attack holds it.
        // Equivalent mutant: dropping the same-enemy test. Only the attacking enemy deals attack
        // damage while its attack is live.
        if (attacker.IsEnemy && world.Attack is { } attack && attack.Enemy == attacker.Card)
        {
            attack.DealtDamage.Record(recipient, damage.Dealt);
            return;
        }
        occurrence.AttackDealtDamage.Record(recipient, damage.Dealt);
        occurrence.Also(AttackDealtDamage);
    }

    internal static void Publish(EnemyAttack attack, Occurrence occurrence)
    {
        if (attack.DealtDamage.Count == 0) return;
        attack.DealtDamage.CopyTo(occurrence.AttackDealtDamage);
        occurrence.Also(AttackDealtDamage);
    }
}
