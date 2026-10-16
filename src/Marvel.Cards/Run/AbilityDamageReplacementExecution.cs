using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Resolves step-one forced replacements individually, rechecking eligibility after each completion.</summary>
internal static class AbilityDamageReplacementExecution
{
    internal static AbilityExecutionResult Begin(AbilityResolutionExecution resolver, World world,
        AbilityExecution entry, DamageReplacement replacement)
    {
        world.Agenda.Executions.RequireBatch(entry);
        if (!ReferenceEquals(entry, replacement.Execution))
            throw new InvalidOperationException("the entry does not own this replacement");
        var events = new List<GameEvent>();
        Continue(resolver, world, replacement, events);
        return entry.Report(events);
    }

    internal static IReadOnlyList<GameEvent> Resume(AbilityResolutionExecution resolver, World world, PhaseStep step)
    {
        var replacement = State(step).Replacement;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(replacement.Execution);
        var events = new List<GameEvent>();
        Continue(resolver, world, replacement, events);
        return events;
    }

    internal static Prompt Choosing(AbilityResolutionExecution resolver, World world, PhaseStep step)
    {
        world.Agenda.Executions.RequireContinuation(step);
        var replacement = State(step).Replacement;
        return new Prompt(world.FirstPlayer, Question.Order, TimingPriority.ForcedInterrupt,
            Steps.DamageWouldBeDealt, "order forced damage replacements", false,
            [.. Pending(resolver, world, replacement).Select(ability => resolver.Describe(world, ability))]);
    }

    internal static IReadOnlyList<GameEvent> Chose(AbilityResolutionExecution resolver, World world,
        PhaseStep step, Decision answer)
    {
        var prompt = Choosing(resolver, world, step);
        var assessment = DecisionAssessment.Assess(prompt, answer);
        if (!assessment.IsAccepted) throw new ArgumentException(assessment.Reason, nameof(answer));
        var replacement = State(step).Replacement;
        var chosen = Pending(resolver, world, replacement).Single(ability =>
            resolver.Describe(world, ability).Id == answer.Affordance);
        world.Agenda.Executions.Resume(replacement.Execution);
        var events = new List<GameEvent>();
        if (Resolve(resolver, world, replacement, chosen, events)) Continue(resolver, world, replacement, events);
        else Suspend(world, replacement, AbilityContinuationStage.Resume, events);
        return events;
    }

    private static void Continue(AbilityResolutionExecution resolver, World world,
        DamageReplacement replacement, List<GameEvent> events)
    {
        while (replacement.CurrentAmount > 0)
        {
            var pending = Pending(resolver, world, replacement);
            if (pending.Count == 0) break;
            if (pending.Count > 1)
            {
                Suspend(world, replacement, AbilityContinuationStage.Choose, events);
                return;
            }
            if (!Resolve(resolver, world, replacement, pending[0], events))
            {
                Suspend(world, replacement, AbilityContinuationStage.Resume, events);
                return;
            }
        }
        replacement.Execution.Finish(events);
    }

    private static List<PendingAbility> Pending(AbilityResolutionExecution resolver,
        World world, DamageReplacement replacement) =>
        resolver.Waiting(world, replacement.Execution.Occurrence, WindowKind.Interrupt)
            .Where(ability => ability.Type == AbilityType.ForcedInterrupt
                && replacement.Execution.Occurrence.MayTrigger(WindowKind.Interrupt, ability)).ToList();

    private static bool Resolve(AbilityResolutionExecution resolver, World world,
        DamageReplacement replacement, PendingAbility ability, List<GameEvent> events)
    {
        var occurrence = replacement.Execution.Occurrence;
        occurrence.Trigger(WindowKind.Interrupt, ability);
        var result = world.Agenda.Executions.Run(occurrence, ability, invocation =>
            resolver.ResolveReplacement(world, invocation, new Decision(ability.OfferId ?? ability.Card, [], []), replacement));
        ActivityBatches.Append(events, result.Events);
        return result.IsComplete;
    }

    private static void Suspend(World world, DamageReplacement replacement,
        AbilityContinuationStage stage, List<GameEvent> events)
    {
        var capture = new DamageReplacementContinuation(replacement) { Execution = replacement.Execution };
        var step = new PhaseStep(new AbilityContinuationAgendaOperation(stage) { Continuation = capture },
            world.Agenda.Current?.Round ?? 0, 1, Subject: replacement.Target.ObjectId, Seat: world.FirstPlayer, Plan: true);
        world.Agenda.ThenContinuation(step, replacement.Execution.Occurrence);
        replacement.Execution.Yield(events);
    }

    private static DamageReplacementContinuation State(PhaseStep step) =>
        step.Require<AbilityContinuationAgendaOperation>().Continuation as DamageReplacementContinuation
            ?? throw new InvalidOperationException("the frame is not a damage replacement continuation");
}
