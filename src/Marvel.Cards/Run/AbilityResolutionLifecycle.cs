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
        foreach (var ability in execution.On(card).Where(ability =>
            ability.Trigger.Timing == AbilityType.WhenRevealed
            && string.Equals(
                ability.Trigger.Event, Steps.CardEntersPlay,
                StringComparison.Ordinal)))
        {
            var cast = new AbilityResolutionState(
                world, card, occurrence, execution.ControllerOf(world, card), events)
            {
                Tier = ability.Trigger.Timing,
            };
            execution.TrackResolution(cast, ability);
            execution.Run(ability, cast);
            cast.CompleteResolution();
        }

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
            foreach (var ability in execution.On(enemy).Where(ability =>
                ability.Trigger.Timing == AbilityType.ForcedResponse
                && string.Equals(
                    ability.Trigger.Event, "WhenActivationCompleted",
                    StringComparison.Ordinal)))
            {
                var cast = new AbilityResolutionState(
                    world, enemy,
                    new Occurrence(
                        0, ["WhenActivationCompleted"],
                        Actor: enemy.ObjectId, Player: result.Player),
                    result.Player, events)
                {
                    Tier = ability.Trigger.Timing,
                };
                execution.TrackResolution(cast, ability);
                execution.Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        foreach (var effect in execution.runtimes.CompleteActivation(world, result.Id))
        {
            var delayedCast = new AbilityResolutionState(
                world,
                world.Cards[effect.Source],
                new Occurrence(
                    0, ["WhenActivationCompleted"],
                    Actor: result.Enemy, Player: effect.Player),
                effect.Player,
                events)
            {
                Tier = effect.Tier,
                AbilityActor = effect.AbilityActor >= 0
                    ? world.Cards[effect.AbilityActor]
                    : null,
            };
            AbilityContinuationCodec.RecordImmediateActivationResult(
                delayedCast.Results, result);
            if (effect.Altered >= 0)
            {
                delayedCast.BindAlteration(world.Cards[effect.Altered]);
            }
            execution.Run(effect.Effect, delayedCast);
        }

        if (world.Agenda.ActivationWait(result.Id) is { } waiting)
        {
            var updated = AbilityContinuationCodec.RecordActivationResult(waiting, result);
            if (updated.Complete)
            {
                _ = world.Agenda.TakeActivationWait(result.Id);
                events.AddRange(execution.ResumeAbility(world, updated.Step));
            }
            else
            {
                world.Agenda.ReplaceActivationWait(result.Id, updated.Step);
            }
        }

        return events;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> ResumeAbility(this AbilityResolutionExecution execution, World world, PhaseStep continuation)
    {
        var source = continuation.Subject >= 0 && continuation.Subject < world.Cards.Count
            ? world.Cards[continuation.Subject]
            : throw new RulesNotImplementedException(
                $"activation continuation has no card at object id {continuation.Subject}");
        var transition = AbilityContinuationCodec.BeginResume(execution.program, source, continuation);
        var resumed = transition switch
        {
            RestartAfterPaidCost paid => paid.State,
            RunResumedNode node => node.State,
            ContinueAfterResumedNode completed => completed.State,
            ResumeComplete complete => complete.State,
            ResumeRejected rejected => throw new RulesNotImplementedException(rejected.Reason),
            _ => throw new InvalidOperationException("Unknown continuation transition"),
        };
        continuation = AbilityContinuationCodec.WithResumedResults(continuation, resumed);

        var cast = execution.Resuming(
            world, source, continuation.Seat, continuation.Tier, continuation.FinalStep,
            continuation.AbilityOccurrence) with
        {
            EachPlayerFrame = continuation.EachPlayerFrame,
            FinalPlayer = continuation.FinalPlayer,
            AbilityPlayer = continuation.AbilityPlayer,
            EventTrigger = continuation.Trigger,
            GainedKeywords = continuation.SurgeGained
                ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal),
        };
        execution.RestoreContinuationCursor(cast, resumed);
        cast.TrackResolution(continuation.AbilityOrdinal);
        execution.RestorePersisted(cast, continuation);
        if (transition is RestartAfterPaidCost paidCost)
        {
            // The cost has settled. Do not persist its resume marker into a
            // later suspension inside the effect, or that continuation would
            // restart the whole effect instead of resuming its own path.
            var ability = paidCost.Ability;
            execution.Use(world, source, ability, cast.Occurrence);
            if (world.Facts.Kind(source.FaceId) == CardKind.Event)
            {
                cast.Occurrence.BeginCard(
                    source.ObjectId,
                    [new PendingAbility(
                        source.ObjectId,
                        ability.Trigger.Timing,
                        continuation.Seat,
                        continuation.AbilityOrdinal)]);
            }
            execution.Run(ability, cast);
            cast.CompleteResolution();
            execution.DiscardEvent(source, cast);
            return cast.Events;
        }
        return execution.ResumeContinuation(cast, source, transition);
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
                    cast.CompleteResolution();
                    execution.DiscardEvent(source, cast);
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
        if (completed.EffectApplied) cast.ResolveEffect();
        return AbilityContinuationCodec.Advance(
            execution.StructuralContext(cast), completed.Ability, completed.State,
            new AbilityStructuralObservation(false));
    }

    private static AbilityContinuationTransition? RunResumedNode(
        this AbilityResolutionExecution execution, AbilityResolutionState cast,
        RunResumedNode run)
    {
        execution.RestoreContinuationCursor(cast, run.State);
        if (run.EffectApplied) cast.ResolveEffect();
        execution.RestoreAlteredFromFrames(cast, run.State.Frames);
        execution.Run(run.Effect, cast);
        if (cast.Suspended)
        {
            cast.CompleteResolution();
            return null;
        }
        return AbilityContinuationCodec.Advance(
            execution.StructuralContext(cast), run.Ability, run.State,
            new AbilityStructuralObservation(false));
    }

    private static AbilityContinuationTransition DiscardForResumedEachTime(
        this AbilityResolutionExecution execution, AbilityResolutionState cast,
        DiscardForResumedEachTime discard)
    {
        execution.RestoreContinuationCursor(cast, discard.State);
        int before = cast.Discarded.Count;
        var one = new AbilityEffect.DiscardTop(
            AbilitySearchArea.EncounterDeck, Players: null,
            new AbilityNumber.Constant(1));
        if (!execution.TryRunCardState(one, cast))
        {
            throw new InvalidOperationException(
                "The card-state owner refused discardTop");
        }
        var discarded = cast.Discarded.Skip(before).SingleOrDefault();
        if (discarded is not null) cast.BindAlteration(discarded);
        return AbilityContinuationCodec.AfterEachTimeDiscard(
            execution.StructuralContext(cast), discard, discarded);
    }

    internal static void RestoreContinuationCursor(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, AbilityContinuationState state)
    {
        cast.RestoreAbility(
            state.Address.Ordinal,
            state.Frames,
            state.Address.Face);
        cast.At(state.Position);
        cast.SetContinuation(state.HasContinuation);
        cast.RestorePlayer(state.Player);
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
