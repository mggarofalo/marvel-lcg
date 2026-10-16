using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Completes forced damage replacement before the optional interrupt tier.</summary>
internal static class DamageWindowPreparation
{
    internal static bool Prepare(World world, PhaseStep step, Occurrence occurrence, List<GameEvent> events)
    {
        if (step.What == Steps.DealAttackDamage) return PrepareDirect(world, step, events);
        if (step.What != Steps.PrepareIndirectAttackDamage) return true;
        long? amount = Attack.PrepareIndirectDamage(world, step, events);
        if (amount is null) return false;
        if (amount > 0) return true;
        if (world.Windows.Current is not null) world.Windows.Close();
        world.Agenda.Cancel(step, occurrence);
        return false;
    }

    private static bool PrepareDirect(World world, PhaseStep step, List<GameEvent> events)
    {
        if (AttackCompletion.Over(world)) return true;
        AttackCompletion.RefreshDefender(world, world.Facts);
        var attack = AttackCompletion.Current(world);
        if (attack.Indirect) return true;
        var operation = step.Require<AttackStageOperation>();
        operation.Replacement ??= DamagePlacement.Replace(world,
            new DamageRequest(world.Cards[attack.Enemy], world.Cards[attack.Target],
                attack.CalculatedDamage ?? throw new InvalidOperationException("attack damage is not calculated"),
                Steps.AttackInitiated, "Attack"), events);
        return operation.Replacement.IsComplete;
    }
}
