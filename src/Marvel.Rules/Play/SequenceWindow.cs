using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Walks agenda interrupt/response windows and their priority statuses.</summary>
internal static class SequenceWindow
{
    internal static Prompt? Work(
        World world, ICardFacts facts, IWindowAbilities abilities,
        List<GameEvent> events, WindowAbilityScope scope, PhaseStep step)
    {
        var kind = world.Agenda.Stage == Stage.Interrupts
            ? WindowKind.Interrupt : WindowKind.Response;
        if (kind == WindowKind.Interrupt && step.What == Steps.RevealEncounterCard)
            EncounterRevealExposure.Prepare(world, world.Cards[step.Subject], step.Seat, events);
        var occurrence = world.Agenda.Begin(world, facts);
        if (!PrepareIndirectWindow(world, step, occurrence, events)) return null;

        var status = new PriorityStatusResolution(world, facts, step, occurrence, kind, events);
        IWindowAbilities offered = step.What == Steps.PrepareIndirectAttackDamage
            ? new OptionalDamageInterrupts(abilities) : abilities;
        Prompt? question = Offering.Work(
            world, offered, occurrence, kind, events, scope, status.Resolve);
        if (question is not null) return AttackPromptContext.WithAttackContext(world, facts, step, question);

        if (WindowIsSuspended(world.Agenda, step, occurrence)) return null;

        FinishWindow(world, step, occurrence, status);
        return null;
    }

    // A suspended triggered ability owns the next work while its original
    // window remains outstanding. Cancellation removes that window instead.
    private static bool WindowIsSuspended(Agenda agenda, PhaseStep owner, Occurrence occurrence) =>
        (agenda.Current is not { } current || !current.SameFrameAs(owner))
        && agenda.IsOutstanding(owner, occurrence);

    private static void FinishWindow(World world, PhaseStep step, Occurrence occurrence,
        PriorityStatusResolution status)
    {
        status.ObserveCancellation();
        if (status.CancelledByStatus)
            CancelAttackWindow(world, step, occurrence, cancelOccurrence: true);
        else if (status.CancelledOccurrence)
            CancelAttackWindow(world, step, occurrence, cancelOccurrence: false);
        else world.Agenda.Advance(occurrence);
    }

    private static bool PrepareIndirectWindow(
        World world, PhaseStep step, Occurrence occurrence, List<GameEvent> events)
    {
        if (step.What != Steps.PrepareIndirectAttackDamage
            || Attack.PrepareIndirectDamage(world, step, events) > 0)
        {
            return true;
        }
        if (world.Windows.Current is not null) world.Windows.Close();
        world.Agenda.Cancel(step, occurrence);
        return false;
    }

    private static void CancelAttackWindow(
        World world, PhaseStep step, Occurrence occurrence, bool cancelOccurrence)
    {
        Attack.CancelPrepared(world, step.Subject);
        world.PendingAdditionalAttackPlayers = [];
        if (world.Windows.Current is not null) world.Windows.Close();
        if (cancelOccurrence) world.Agenda.Cancel(step, occurrence);
    }

    private sealed class PriorityStatusResolution(
        World world, ICardFacts facts, PhaseStep step, Occurrence occurrence,
        WindowKind kind, List<GameEvent> events)
    {
        public bool CancelledByStatus { get; private set; }
        public bool CancelledOccurrence { get; private set; }

        public bool Resolve()
        {
            if (!world.Agenda.IsOutstanding(step, occurrence))
            {
                CancelledOccurrence = true;
                return true;
            }
            CancelledByStatus = kind == WindowKind.Interrupt
                && step.What == Steps.Attack
                && BasicPowerStatus.Cancelled(
                    world, facts, world.Cards[step.Subject], Statuses.Stunned, events);
            if (!CancelledByStatus && kind == WindowKind.Interrupt && step.What == Steps.Attack)
            {
                Attack.Prepare(world, facts, step);
            }
            return CancelledByStatus;
        }

        public void ObserveCancellation() =>
            CancelledOccurrence |= !world.Agenda.IsOutstanding(step, occurrence);
    }

}
