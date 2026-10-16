using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Initiates an enemy attack and schedules its rule-defined steps.</summary>
internal static class AttackInitiation
{
    internal static void Initiate(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:stun-stunned.1`: "**Forced Interrupt**: when this character would
        // attack, remove each stunned status card from it instead." *Instead*
        // -- so the attack does not happen at all, and none of its six steps is
        // scheduled. No boost card is given and no defender is asked for.
        if (BasicPowerStatus.Cancelled(
            world, facts, world.Cards[step.Subject], Statuses.Stunned, events))
        {
            CancelPrepared(world, step.Subject);
            world.PendingAdditionalAttackPlayers = [];
            return;
        }

        // `rr:attack-enemy-activation.1` -- against both a player and a
        // character, and `.1.1` -- "normally the attacked character
        // is the player's hero, but abilities can instead cause an enemy to
        // attack a player's alter-ego or an ally that player controls", and
        // `rr:attacks-against-allies.1` keeps the player attacked either way,
        // so the seat is unchanged and only the character moves.
        Prepare(world, facts, step);
        var additional = world.PendingAdditionalAttackPlayers;
        world.PendingAdditionalAttackPlayers = [];
        world.Attack = Current(world) with { AdditionalPlayers = additional };

        // `rr:activation` -- "whenever an enemy attacks or schemes, it is
        // considered to have activated". The umbrella, which a scheme sets too;
        // `world.Attack` is the six steps below it.
        world.Activation ??= new EnemyActivation(
            step.Subject, step.Seat, Attacking: true, Id: step.ActivationId);

        world.Agenda.Then(new PhaseStep(
            new AttackStageOperation(AttackStageKind.GiveBoostCard),
            step.Round,
            1,
            Index: step.Seat,
            Subject: step.Subject
        ) { ActivationId = step.ActivationId });
        world.Agenda.Then(new PhaseStep(
            new AttackStageOperation(AttackStageKind.DeclareDefender),
            step.Round,
            2,
            Index: step.Seat,
            Subject: step.Subject,
            Seat: step.Seat
        ) { ActivationId = step.ActivationId });
        world.Agenda.Then(new PhaseStep(
            new AttackStageOperation(AttackStageKind.FlipBoostCards),
            step.Round,
            3,
            Index: step.Seat,
            Subject: step.Subject
        ) { ActivationId = step.ActivationId });
        world.Agenda.Then(new PhaseStep(
            new AttackStageOperation(AttackStageKind.CalculateAttackDamage),
            step.Round,
            4,
            Index: step.Seat,
            Subject: step.Subject,
            Seat: step.Seat
        ) { ActivationId = step.ActivationId });
        world.Agenda.Then(new PhaseStep(
            new AttackStageOperation(AttackStageKind.DealAttackDamage),
            step.Round,
            5,
            Index: step.Seat,
            Subject: step.Subject,
            Seat: step.Seat
        ) { ActivationId = step.ActivationId });
        foreach (int player in additional)
        {
            world.Agenda.Then(new PhaseStep(
                new AttackStageOperation(AttackStageKind.NextAttackTarget),
                step.Round,
                5,
                Index: player,
                Subject: step.Subject,
                Seat: player,
                Plan: true
            ) { ActivationId = step.ActivationId });
            world.Agenda.Then(new PhaseStep(
                new AttackStageOperation(AttackStageKind.DeclareDefender),
                step.Round,
                2,
                Index: player,
                Subject: step.Subject,
                Seat: player
            ) { ActivationId = step.ActivationId });
            world.Agenda.Then(new PhaseStep(
                new AttackStageOperation(AttackStageKind.CalculateAttackDamage),
                step.Round,
                4,
                Index: player,
                Subject: step.Subject,
                Seat: player
            ) { ActivationId = step.ActivationId });
            world.Agenda.Then(new PhaseStep(
                new AttackStageOperation(AttackStageKind.DealAttackDamage),
                step.Round,
                5,
                Index: player,
                Subject: step.Subject,
                Seat: player
            ) { ActivationId = step.ActivationId });
        }
        world.Agenda.Then(new PhaseStep(
            new AttackCompletionOperation(
                Owner: Occurrence.ForAttack(
                world.Agenda.Occurrence?.Id ?? Moment.Id(step.Round, step.Number, step.Index),
                [], world, facts, step.Subject, world.Attack.Target, step.Seat)
            ),
            step.Round,
            6,
            Index: step.Seat,
            Subject: step.Subject,
            Seat: step.Seat
        ) { ActivationId = step.ActivationId });
    }

}
