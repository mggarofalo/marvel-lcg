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
            occurrence.Also(Steps.DamageDealt);
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
