using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Completes forced damage replacement before the optional interrupt tier.</summary>
internal static class DamageWindowPreparation
{
    internal static bool Prepare(World world, PhaseStep step, Occurrence occurrence, List<GameEvent> events)
    {
        if (step.What == Steps.DealAttackDamage) return PrepareDirect(world, step, occurrence, events);
        if (step.What != Steps.PrepareIndirectAttackDamage) return true;
        long? amount = Attack.PrepareIndirectDamage(world, step, events);
        occurrence.Damage = step.Require<IndirectDamageOperation>().Replacement;
        if (amount is null) return false;
        if (amount > 0) return true;
        if (world.Windows.Current is not null) world.Windows.Close();
        world.Agenda.Cancel(step, occurrence);
        return false;
    }

    private static bool PrepareDirect(World world, PhaseStep step, Occurrence occurrence, List<GameEvent> events)
    {
        if (AttackCompletion.Over(world)) return true;
        AttackCompletion.RefreshDefender(world, world.Facts);
        var attack = AttackCompletion.Current(world);
        if (attack.Indirect) return true;
        var operation = step.Require<AttackStageOperation>();
        if (operation.Replacement is null)
            DiscardPiercedTough(world, attack, events);
        operation.Replacement ??= DamagePlacement.Replace(world,
            new DamageRequest(world.Cards[attack.Enemy], world.Cards[attack.Target],
                attack.CalculatedDamage ?? throw new InvalidOperationException("attack damage is not calculated"),
                Steps.AttackInitiated, "Attack") { FromAttack = true }, events);
        occurrence.Damage = operation.Replacement;
        return operation.Replacement.IsComplete;
    }

    // rr:piercing.1 discards each tough status card "before this attack deals damage
    // to a character", so the "would take" interrupts of rr:damage step 3 see the
    // damage Tough no longer prevents. DiscardPiercedTough applies rr:piercing.2.
    private static void DiscardPiercedTough(World world, EnemyAttack attack, List<GameEvent> events)
    {
        var enemy = world.Cards[attack.Enemy];
        if (AttackKeywords.Has(world, enemy, enemy, Keywords.Piercing))
            DamageAttacks.DiscardPiercedTough(world, world.Cards[attack.Target], enemy,
                attack.CalculatedDamage ?? 0, Steps.AttackInitiated, events);
    }
}
