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
        if (SharedThreatWindowHandles(step, kind))
        {
            world.Agenda.Advance();
            return null;
        }
        if (kind == WindowKind.Interrupt && step.What == Steps.RevealEncounterCard)
            EncounterRevealExposure.Prepare(world, world.Cards[step.Subject], step.Seat, events);
        var occurrence = world.Agenda.Begin(world, facts);
        if (kind == WindowKind.Interrupt
            && !DamageWindowPreparation.Prepare(world, step, occurrence, events)) return null;

        var status = new PriorityStatusResolution(world, facts, step, occurrence, kind, events);
        var offered = AbilitiesForStep(step, abilities);
        Prompt? question = Offering.Work(
            world, offered, occurrence, kind, events, scope, status.Resolve);
        if (question is not null) return AttackPromptContext.WithAttackContext(world, facts, step, question);

        if (world.Agenda.Executions.Blocks(occurrence)) return null;

        FinishWindow(world, step, occurrence, status);
        return null;
    }

    private static IWindowAbilities AbilitiesForStep(PhaseStep step, IWindowAbilities abilities) =>
        step.What is Steps.PrepareIndirectAttackDamage or Steps.DealAttackDamage
            ? new OptionalDamageInterrupts(abilities) : abilities;

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

    private static void CancelAttackWindow(
        World world, PhaseStep step, Occurrence occurrence, bool cancelOccurrence)
    {
        Attack.CancelPrepared(world, step.Subject);
        world.PendingAdditionalAttackPlayers = [];
        if (world.Windows.Current is not null) world.Windows.Close();
        if (cancelOccurrence) world.Agenda.Cancel(step, occurrence);
    }

    internal static void Answer(
        World world, ICardFacts facts, Prompt asked, Decision input, List<GameEvent> events)
    {
        IWindowAbilities abilities = world.WindowAbilities;
        var window = world.Windows.Current
            ?? throw new InvalidOperationException("the bound window is no longer open");
        if (input.IsDecline)
        {
            DeclineWindow(world, asked, window);
            return;
        }

        PendingAbility ability = AcceptedAbility(world, abilities, window, asked, input);

        window.Occurrence.Trigger(window.Kind, ability);

        // `rr:initiating-abilities.step.5` -- what the player spent is part of
        // the answer, not something the engine picks for them. Empty when the
        // affordance was free, which is almost all of them.
        var healthBefore = world.Effects.CaptureCharacterHealth();
        ActivityBatches.Append(events, abilities.ResolveWindow(
            world, window.Occurrence, ability, input).Events);
        Statuses.RemoveAfflictionsIfStalwart(
            world, facts, "stalwart", events);
        world.Effects.SettleLostHealth(healthBefore, asked.Trigger, events);

        if (window.Occurrence.Threat is { Replaced: true })
        {
            // `rr:replacement-effect.1`: the original effect is no longer
            // imminent, so it has neither further interrupts nor responses.
            world.Windows.Close();
            world.Agenda.Cancel(window.Occurrence);
            return;
        }

        // Not a close: rr:interrupt.5 is about *further* abilities, so using one
        // gives everybody another opportunity and the step stays where it is.
        world.Windows.Used();
    }

    private static bool SharedThreatWindowHandles(PhaseStep step, WindowKind kind) =>
        (kind == WindowKind.Response && step.Operation is ThreatPlacementOperation { Responses: not null })
        || (kind == WindowKind.Interrupt && step.Operation is CompletedCardEffectOperation { Occurrence: not null });

    private static PendingAbility AcceptedAbility(
        World world, IWindowAbilities abilities, Window window, Prompt asked, Decision input)
    {
        var taken = Offering.CurrentlyAvailable(world, abilities, window, asked.Player)
            .Select(ability => (Ability: ability, Offered: abilities.Describe(world, ability)))
            .Where(pair => pair.Offered.Id == input.Affordance
                && (asked.Asking != Question.Opportunity
                    || pair.Ability.Player == asked.Player
                    || pair.Ability.Player < 0))
            .Select(pair => (PendingAbility?)pair.Ability)
            .FirstOrDefault();

        if (taken is not { } ability)
        {
            throw new RulesNotImplementedException(
                $"affordance {input.Affordance} is not on offer in '{asked.Label}'");
        }

        return ability;
    }

    private static void DeclineWindow(World world, Prompt asked, Window window)
    {
        // "Forced Interrupt" and "Forced Response" abilities must resolve (rr:forced.1).
        if (!asked.Cancellable)
            throw new RulesNotImplementedException($"'{asked.Label}' cannot be declined");
        Passed(world, window.Occurrence, world.Windows.Pass());
    }
    // Answering the last question of a window finishes that part of the step.
    // Without this the walk would find no window open, take that for "not yet
    // opened", and ask the same question again.
    private static void Passed(World world, Occurrence occurrence, bool closed)
    {
        if (closed && world.Agenda.IsBusy)
        {
            world.Agenda.Advance(occurrence);
        }
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
