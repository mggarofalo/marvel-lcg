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
    internal static void RecordDamage(World world, AttackDamageRequest request, AbilityExecution execution,
        OccurrenceCard attacker, Damage.PlacedDamage damage, bool spillOnly, List<GameEvent> events)
    {
        RecordAttackPlacement(world, request, execution, attacker, damage, spillOnly);
        RecordCausalDamage(world, request, damage, spillOnly);
        AttackDealtDamageTiming.Record(world, attacker, damage, execution.Occurrence);
        DamageAttacks.ResolveLandedDamage(world, request.ActivationId, request.Target, damage, events);
    }

    private static void RecordAttackPlacement(World world, AttackDamageRequest request, AbilityExecution execution,
        OccurrenceCard attacker, Damage.PlacedDamage damage, bool spillOnly)
    {
        if (request.EnemyAttack is not null || spillOnly)
            RecordPlacement(world, request.ActivationId, damage);
        if (damage.Landed && request.EnemyAttack is null && !spillOnly)
            execution.Occurrence.Also(Steps.DamageDealt);
        RecordReceived(world, request, execution, attacker, damage, spillOnly);
    }

    private static void RecordReceived(World world, AttackDamageRequest request, AbilityExecution execution,
        OccurrenceCard attacker, Damage.PlacedDamage damage, bool spillOnly)
    {
        if (damage.Landed && attacker.IsEnemy && (request.EnemyAttack is not null || spillOnly))
        {
            AttackDamageReceipts.Record(world, attacker, damage);
            world.Attack?.Defeats.Register(execution.Occurrence, request.Target);
        }
    }

    private static void RecordCausalDamage(World world, AttackDamageRequest request, Damage.PlacedDamage damage, bool spillOnly)
    {
        if (!spillOnly && request.EnemyAttack is { } attack)
            RecordAttackDamage(world, attack, damage);
        RecordRecipient(world, request.Source, request.Target, damage.Dealt);
        if (request.EnemyAttack is not null || spillOnly)
            RecordIdentityDamage(world, request.Source, damage);
    }

    internal static void ValidateBoostPlacement(World world, int? activationId)
    {
        if (activationId is null) return;
        if (activationId < 0 || world.Activation?.Id != activationId)
            throw new InvalidOperationException("boost placement has no live activation receipt");
    }

    internal static void RecordPlacement(World world, int? activationId, Damage.PlacedDamage damage)
    {
        // rr:damage.step.5 places damage after prevention; rr:damage.2 places
        // the specified tokens on allies, including damage beyond their hit points.
        // rr:overkill.2 makes the spill damage part of the same attack.
        if (activationId is not >= 0 || !damage.Landed) return;
        if (world.Activation is { } active && active.Id == activationId)
            world.Activation = active with { DamagePlaced = active.DamagePlaced + damage.Taken };
        else if (world.FinishedActivation is { } finished && finished.Id == activationId)
            world.FinishedActivation = finished with { DamagePlaced = finished.DamagePlaced + damage.Taken };
        else
            throw new InvalidOperationException("attack placement has no matching activation receipt");
    }

    internal static void RecordAttackDamage(World world, EnemyAttack attack, Damage.PlacedDamage damage)
    {
        // Record placement before defeat can end the activation and emit its result.
        if (damage.Landed) MarkAttackDamaged(world, attack);
        AddActivationDamage(world, damage.Dealt);
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

    internal static void RecordRecipient(World world, Card source, Card target, long dealt)
    {
        if (dealt <= 0 || !world.Seats.Any(seat => seat.IdentityCard == target)) return;
        if (world.Activation is { } active && active.Enemy == source.ObjectId)
            world.Activation = AddRecipient(active, target.ObjectId);
        else if (world.FinishedActivation is { } finished && finished.Enemy == source.ObjectId)
            world.FinishedActivation = AddRecipient(finished, target.ObjectId);
    }

    internal static void RecordIdentityDamage(World world, Card source, Damage.PlacedDamage damage)
    {
        if (!damage.Landed || !world.Seats.Any(seat => seat.IdentityCard == damage.Target)) return;
        if (world.Attack is { } active && active.Enemy == source.ObjectId)
            world.Attack = AddIdentity(active, damage.Target.ObjectId);
        else if (world.FinishedAttack is { } finished && finished.Enemy == source.ObjectId)
            world.FinishedAttack = AddIdentity(finished, damage.Target.ObjectId);
    }

    private static EnemyAttack AddIdentity(EnemyAttack attack, int target) =>
        attack.DamagedIdentities.Contains(target) ? attack
            : attack with { DamagedIdentities = [.. attack.DamagedIdentities, target] };

    private static EnemyActivation AddRecipient(EnemyActivation activation, int target) =>
        activation.DamageRecipients.Contains(target) ? activation
            : activation with { DamageRecipients = [.. activation.DamageRecipients, target] };

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
        }
        if ((world.Activation ?? world.FinishedActivation) is { } activation)
            foreach (var damage in placed)
            {
                RecordRecipient(world, world.Cards[activation.Enemy], damage.Target, damage.Dealt);
                RecordIdentityDamage(world, world.Cards[activation.Enemy], damage);
            }
        AddActivationDamage(world, placed.Sum(damage => damage.Dealt));
    }

}
