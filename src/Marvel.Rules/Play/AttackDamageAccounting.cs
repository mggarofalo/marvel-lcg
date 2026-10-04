using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Records actual attack-step damage independently of placement and defeat.</summary>
internal static class AttackDamageAccounting
{
    internal static void RecordAttackDamage(
        World world, EnemyAttack attack, Damage.AttackResult damage,
        List<GameEvent> events, int firstDamageEvent)
    {
        if (damage.Characters.Count > 0)
        {
            MarkAttackDamaged(world, attack);
            world.Agenda.Occurrence?.Also(Steps.DamageDealt);
        }
        AddActivationDamage(world, damage.Dealt);
        FinalizeEarlyCompletion(world, attack, events, firstDamageEvent);
    }

    private static void FinalizeEarlyCompletion(
        World world, EnemyAttack attack, List<GameEvent> events, int firstDamageEvent)
    {
        if (world.Attack is not null || world.FinishedActivation is not { } activation) return;
        // Identity elimination can end the attack inside damage placement.
        // Complete the emitted result after that primitive returns its actual
        // dealt amount. Only completions emitted by this placement are eligible;
        // prior attacks and boost-only terminations retain their own results.
        for (int index = firstDamageEvent; index < events.Count; index++)
        {
            if (events[index] is AttackCompleted completed && completed.Enemy == attack.Enemy)
                events[index] = completed with { DamageDealt = activation.DamageDealt };
        }
    }

    private static void MarkAttackDamaged(World world, EnemyAttack attack)
    {
        if (world.Attack is not null)
        {
            world.Attack = attack with { Damaged = true };
        }
        else if (world.FinishedAttack is { } finishedAttack)
        {
            world.FinishedAttack = finishedAttack with { Damaged = true };
        }
    }

    private static void AddActivationDamage(World world, long dealt)
    {
        if (world.Activation is { } activation)
        {
            world.Activation = activation with { DamageDealt = activation.DamageDealt + dealt };
        }
        else if (world.FinishedActivation is { } finishedActivation)
        {
            world.FinishedActivation = finishedActivation with
            {
                DamageDealt = finishedActivation.DamageDealt + dealt,
            };
        }
    }

    internal static void RecordIndirectDamage(
        World world, Occurrence occurrence, IReadOnlyList<Damage.PlacedDamage> placed)
    {
        if (placed.Any(damage => damage.Landed))
        {
            if (world.Attack is { } currentAttack)
            {
                world.Attack = currentAttack with { Damaged = true };
            }
            else if (world.FinishedAttack is { } finishedAttack)
            {
                world.FinishedAttack = finishedAttack with { Damaged = true };
            }
            occurrence.Also(Steps.DamageDealt);
        }
        AddActivationDamage(world, placed.Sum(damage => damage.Dealt));
    }

}
