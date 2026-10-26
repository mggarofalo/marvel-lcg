using static Marvel.Cards.Run.AbilityContinuationFrames;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionLifecycle
{
    internal static IReadOnlyList<GameEvent> EntersPlay(this AbilityResolutionExecution execution, World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var events = new List<GameEvent>();
        var occurrence = new Occurrence(
            0, [Steps.CardEntersPlay], Subject: card.ObjectId,
            Player: execution.ControllerOf(world, card));
        execution.RunOrdered(AbilityProgramQueries.ActiveOn(world, execution.program, card).Where(ability =>
            ability.Trigger.Timing == AbilityType.WhenRevealed
            && string.Equals(ability.Trigger.Event, Steps.CardEntersPlay, StringComparison.Ordinal)),
            ability => new AbilityResolutionState(
                world, card, occurrence, execution.ControllerOf(world, card), events)
            {
                Tier = ability.Trigger.Timing,
            });

        return events;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> ActivationCompleted(this AbilityResolutionExecution execution, World world, EnemyActivation result)
    {
        ArgumentNullException.ThrowIfNull(world);

        var events = new List<GameEvent>();
        var enemy = world.Cards[result.Enemy];
        if (result.Made)
        {
            var occurrence = new Occurrence(0, ["WhenActivationCompleted"],
                Actor: enemy.ObjectId, Player: result.Player);
            execution.RunOrdered(AbilityProgramQueries.ActiveOn(world, execution.program, enemy).Where(ability =>
                ability.Trigger.Timing == AbilityType.ForcedResponse
                && string.Equals(ability.Trigger.Event, "WhenActivationCompleted", StringComparison.Ordinal)),
                ability => new AbilityResolutionState(world, enemy, occurrence, result.Player, events)
                {
                    Tier = ability.Trigger.Timing,
                });
        }

        AbilityDelayedExecution.CompleteActivation(execution, world, result, events);

        if (world.Agenda.ActivationWait(result.Id) is { } waiting)
        {
            var updated = AbilityContinuations.RecordActivationResult(waiting, result);
            world.Agenda.ReplaceActivationWait(result.Id, updated.Step);
        }

        return events;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> ResumeAbility(this AbilityResolutionExecution execution, World world, PhaseStep continuation)
    {
        world.Agenda.Executions.RequireContinuation(continuation);
        var source = continuation.Subject >= 0 && continuation.Subject < world.Cards.Count
            ? world.Cards[continuation.Subject]
            : throw new RulesNotImplementedException(
                $"activation continuation has no card at object id {continuation.Subject}");
        bool delayed = StateOf(continuation)?.Reason == AbilityResumeReason.DelayedEffect;
        var transition = AbilityContinuations.BeginResume(execution.program, source, continuation);
        var resumed = transition switch
        {
            RestartAfterPaidCost paid => paid.State,
            RunResumedNode node => node.State,
            ContinueAfterResumedNode completed => completed.State,
            ResumeComplete complete => complete.State,
            ResumeRejected rejected => throw new RulesNotImplementedException(rejected.Reason),
            _ => throw new InvalidOperationException("Unknown continuation transition"),
        };
        continuation = AbilityContinuations.WithResumedResults(continuation, resumed);

        var cast = CreateResumedState(execution, world, source, continuation, resumed);
        execution.RestoreContinuationCursor(cast, resumed);
        execution.RestorePersisted(cast, continuation);
        if (delayed)
            cast.EnterExecution(world.Agenda.Executions.BeginBatch(cast.Occurrence));
        cast.TrackResolution(resumed.Invocation.Address.Ordinal);
        if (!AbilityPaymentResumption.Complete(execution, cast, resumed)) return cast.Events;
        if (transition is RestartAfterPaidCost paidCost)
        {
            // The cost has settled. Do not persist its resume marker into a
            // later suspension inside the effect, or that continuation would
            // restart the whole effect instead of resuming its own path.
            var ability = paidCost.Ability;
            execution.Use(world, source, ability, cast.Occurrence);
            if (EffectiveCards.Kind(source, world.Facts) == CardKind.Event)
            {
                cast.Occurrence.Resolution.BeginCard(
                    source.ObjectId,
                    [cast.ResolutionAbility!.Value]);
            }
            if (!AbilityRecipientCommitment.Resume(execution, cast, ability))
                execution.Run(ability, cast);
            execution.DiscardEvent(source, cast);
            cast.CompleteResolution();
            return cast.Events;
        }
        return execution.ResumeContinuation(cast, source, transition);
    }

    /// <summary>Restores invocation and active power facts for procedure and choice resumes.</summary>
    internal static AbilityResolutionState CreateResumedState(AbilityResolutionExecution execution,
        World world, Card source, PhaseStep continuation, AbilityContinuationState resumed)
    {
        return execution.Resuming(
            world, source, continuation.Seat, resumed.Invocation.Address.Tier, resumed.Cursor.FinalStep,
            resumed.Invocation.Occurrence) with
        {
            DamageDefeats = resumed.Memory.DamageDefeats,
            Power = resumed.PowerExecution?.Kind,
            PowerAmount = resumed.PowerExecution?.Amount ?? -1,
            PowerActor = resumed.PowerExecution?.Actor is { } actor ? world.Cards[actor] : null,
            PowerTargets = resumed.PowerExecution?.Continuation.Targets.Select(id => world.Cards[id]).ToArray() ?? [],
            ThwartActorUsingAttack = resumed.PowerExecution?.ThwartActorUsingAttack,
            EachPlayerFrame = resumed.Cursor.EachPlayerFrame,
            FinalPlayer = resumed.Cursor.FinalPlayer,
            AbilityPlayer = resumed.Invocation.AbilityPlayer,
            EventTrigger = resumed.Invocation.Trigger,
            GainedKeywords = resumed.Memory.SurgeGained
                ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal),
        };
    }

    internal static List<GameEvent> ResumeContinuation(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, Card source, AbilityContinuationTransition transition)
    {
        while (true)
        {
            switch (transition)
            {
                case ContinueAfterResumedNode completed:
                    transition = execution.ContinueAfterResumedNode(cast, completed);
                    break;

                case RunResumedNode run:
                    var resumed = execution.RunResumedNode(cast, run);
                    if (resumed is null) return cast.Events;
                    transition = resumed;
                    break;

                case DiscardForResumedEachTime discard:
                    transition = execution.DiscardForResumedEachTime(cast, discard);
                    break;

                case ResumeComplete complete:
                    execution.RestoreContinuationCursor(cast, complete.State);
                    if (!cast.Suspended && cast.PowerExecution is not null)
                    {
                        execution.FinishResumedPower(cast);
                        return cast.Events;
                    }
                    execution.DiscardEvent(source, cast);
                    cast.CompleteResolution();
                    return cast.Events;

                case ResumeRejected rejected:
                    throw new RulesNotImplementedException(rejected.Reason);

                default:
                    throw new InvalidOperationException(
                        $"Unknown continuation transition {transition.GetType().Name}");
            }
        }
    }

    private static AbilityContinuationTransition ContinueAfterResumedNode(
        this AbilityResolutionExecution execution, AbilityResolutionState cast,
        ContinueAfterResumedNode completed)
    {
        execution.RestoreContinuationCursor(cast, completed.State);
        AbilityResumedEffectCompletion.Apply(cast, completed);
        return AbilityContinuations.Advance(
            execution.StructuralContext(cast), completed.Ability, completed.State,
            new AbilityStructuralObservation(false, Outcome: cast.CompletedOutcome));
    }

    private static AbilityContinuationTransition? RunResumedNode(
        this AbilityResolutionExecution execution, AbilityResolutionState cast,
        RunResumedNode run)
    {
        execution.RestoreContinuationCursor(cast, run.State);
        if (run.EffectApplied) cast.ResolveEffect();
        execution.RestoreAlteredFromFrames(cast, run.State.Cursor.Frames);
        execution.Run(run.Effect, cast);
        if (cast.Suspended)
        {
            cast.CompleteResolution();
            return null;
        }
        return AbilityContinuations.Advance(
            execution.StructuralContext(cast), run.Ability, run.State,
            new AbilityStructuralObservation(false, Outcome: cast.CompletedOutcome));
    }

    private static AbilityContinuationTransition DiscardForResumedEachTime(
        this AbilityResolutionExecution execution, AbilityResolutionState cast,
        DiscardForResumedEachTime discard)
    {
        execution.RestoreContinuationCursor(cast, discard.State);
        var discarded = execution.EachTimeDiscard(cast, discard.Frame);
        if (discarded is not null) cast.BindAlteration(discarded);
        return AbilityContinuations.AfterEachTimeDiscard(
            execution.StructuralContext(cast), discard, discarded);
    }

    internal static Card? EachTimeDiscard(
        this AbilityResolutionExecution execution, AbilityResolutionState cast, EachTimeFrame frame)
    {
        if (!frame.Receipt.IsDefault)
            return cast.World.Cards[frame.Receipt[checked((int)frame.Next)]];
        int before = cast.Discarded.Count;
        var one = new AbilityEffect.DiscardTop(
            AbilitySearchArea.EncounterDeck, Players: null,
            new AbilityNumber.Constant(1));
        if (!execution.TryRunCardState(one, cast))
        {
            throw new InvalidOperationException(
                "The card-state owner refused discardTop");
        }
        return cast.Discarded.Skip(before).SingleOrDefault();
    }

    internal static void RestoreContinuationCursor(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, AbilityContinuationState state)
    {
        cast.Execution ??= state.Execution;
        cast.TriggeringDefeat = state.Invocation.TriggeringDefeat;
        cast.TriggeringAttack = state.Invocation.TriggeringAttack;
        cast.TriggeringThreat = state.Invocation.TriggeringThreat;
        cast.RestoreAbility(
            state.Invocation.Address.Ordinal,
            state.Cursor.Frames,
            state.Invocation.Address.Face);
        cast.At(state.Cursor.Position);
        cast.SetContinuation(state.Cursor.HasContinuation);
        cast.RestorePlayer(state.Cursor.Player);
    }

    internal static void RestoreAlteredFromFrames(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, ImmutableArray<AbilityStructuralFrame> frames)
    {
        var card = frames.OfType<EachTimeFrame>()
            .LastOrDefault()?.DiscardedCard;
        if (card is { } id)
            cast.BindAlteration(cast.World.Cards[id]);
    }
}
