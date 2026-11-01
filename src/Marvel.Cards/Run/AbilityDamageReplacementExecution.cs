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
        if (Pending(resolver, world, replacement) is { Count: > 0 } forced)
            return new Prompt(world.FirstPlayer, Question.Order, TimingPriority.ForcedInterrupt,
                Steps.DamageWouldBeDealt, "order forced damage replacements", false,
                [.. forced.Select(ability => Describe(resolver, world, replacement, ability))]);
        var optional = Offered(resolver, world, replacement);
        return new Prompt(optional[0].Player, Question.Opportunity, TimingPriority.Interrupt,
            Steps.DamageWouldBeDealt, "interrupt before damage is dealt", true,
            [.. optional.Select(ability => Describe(resolver, world, replacement, ability))])
        { DisplayQuestion = "Interrupt before damage is dealt?" };
    }

    internal static IReadOnlyList<GameEvent> Chose(AbilityResolutionExecution resolver, World world,
        PhaseStep step, Decision answer)
    {
        var prompt = Choosing(resolver, world, step);
        var assessment = DecisionAssessment.Assess(prompt, answer);
        if (!assessment.IsAccepted) throw new ArgumentException(assessment.Reason, nameof(answer));
        var replacement = State(step).Replacement;
        var events = new List<GameEvent>();
        if (answer.IsDecline)
        {
            // Passing an opportunity spends it for this occurrence.
            foreach (var passed in Offered(resolver, world, replacement))
                replacement.Execution.Occurrence.Trigger(WindowKind.Interrupt, passed);
            world.Agenda.Executions.Resume(replacement.Execution);
            Continue(resolver, world, replacement, events);
            return events;
        }
        var choices = Pending(resolver, world, replacement) is { Count: > 0 } forced ? forced : Offered(resolver, world, replacement);
        var chosen = choices.Single(ability =>
            Describe(resolver, world, replacement, ability).Id == answer.Affordance);
        world.Agenda.Executions.Resume(replacement.Execution);
        if (Resolve(resolver, world, replacement, chosen, events)) Continue(resolver, world, replacement, events);
        else Suspend(world, replacement, AbilityContinuationStage.Resume, events);
        return events;
    }

    private static void Continue(AbilityResolutionExecution resolver, World world,
        DamageReplacement replacement, List<GameEvent> events)
    {
        if (!Reflect(world, replacement, events))
        {
            Suspend(world, replacement, AbilityContinuationStage.Resume, events);
            return;
        }
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
        if (replacement.CurrentAmount > 0 && Offered(resolver, world, replacement).Count > 0)
        {
            Suspend(world, replacement, AbilityContinuationStage.Choose, events);
            return;
        }
        if (replacement.PreventsAll) replacement.PreventAll(replacement.Execution);
        replacement.Execution.Finish(events);
    }

    /// <summary>Prevents attack damage reflected to the attacker and deals it there; false while that damage is unfinished.</summary>
    private static bool Reflect(World world, DamageReplacement replacement, List<GameEvent> events)
    {
        if (replacement.CurrentAmount <= 0 || replacement.PreventsAll
            || PlayerAttackReflection.For(world, replacement.Target) is not { } reflection) return true;
        long amount = replacement.CurrentAmount;
        replacement.PreventAll(replacement.Execution);
        return DamagePlacement.Resolve(world, world.Facts,
            new(reflection.Source, reflection.Recipient, amount, reflection.Trigger, "Damage"), events).IsComplete;
    }

    // Equivalent mutant: forced replacements have already triggered when this list is
    // read, only the attacking player's Follow Through matches "your hero's attack",
    // and no excess remains at 0 damage, so dropping the type, player or amount
    // filters survives.
    // rr:damage step 1 also opens to optional interrupts when an attack would deal
    // excess damage (Follow Through, 16045). Forced replacements resolve first
    // (rr:ability.step.2); the first player with an opportunity is asked.
    private static List<PendingAbility> Offered(AbilityResolutionExecution resolver,
        World world, DamageReplacement replacement)
    {
        var occurrence = replacement.Execution.Occurrence;
        if (!occurrence.Is(ExcessDamageTiming.AttackExcessDamageWouldBeDealt)) return [];
        var optional = AbilityOfferHandles.Assign(resolver.Waiting(world, occurrence, WindowKind.Interrupt), world.Cards.Count)
            .Where(ability => ability.Type == AbilityType.Interrupt && occurrence.MayTrigger(WindowKind.Interrupt, ability)).ToList();
        return optional.Count == 0 ? optional : [.. optional.Where(ability => ability.Player == optional[0].Player)];
    }

    private static List<PendingAbility> Pending(AbilityResolutionExecution resolver,
        World world, DamageReplacement replacement)
    {
        if (replacement.PreventsAll) return [];
        var authored = resolver.Waiting(world, replacement.Execution.Occurrence, WindowKind.Interrupt);
        var native = GreenGoblinDamageReplacement.Waiting(world, replacement);
        return AbilityOfferHandles.Assign(native is { } pending
                ? authored.Append(pending) : authored, world.Cards.Count)
            .Where(ability => ability.Type == AbilityType.ForcedInterrupt
                && replacement.Execution.Occurrence.MayTrigger(WindowKind.Interrupt, ability)).ToList();
    }

    private static Affordance Describe(AbilityResolutionExecution resolver, World world,
        DamageReplacement replacement, PendingAbility ability) =>
        ability.Native == NativeAbilityKind.GreenGoblinDamageReplacement
            ? GreenGoblinDamageReplacement.Describe(world, ability, replacement)
            : resolver.Describe(world, ability);

    private static bool Resolve(AbilityResolutionExecution resolver, World world,
        DamageReplacement replacement, PendingAbility ability, List<GameEvent> events)
    {
        var occurrence = replacement.Execution.Occurrence;
        occurrence.Trigger(WindowKind.Interrupt, ability);
        var result = world.Agenda.Executions.Run(occurrence, ability, invocation =>
            ability.Native == NativeAbilityKind.GreenGoblinDamageReplacement
                ? GreenGoblinDamageReplacement.Resolve(world, invocation, replacement)
                : resolver.ResolveReplacement(world, invocation,
                    new Decision(ability.OfferId ?? ability.Card, [], []), replacement));
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
