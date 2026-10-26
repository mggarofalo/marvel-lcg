using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

using static Marvel.Cards.Run.AbilityStructuralExecution;
using static Marvel.Cards.Run.AbilityStructuralPowerExecution;
using static Marvel.Cards.Run.AbilityStructuralFlowExecution;
namespace Marvel.Cards.Run;

// One immutable read of a live resolution. The executor refreshes it after every
// command, so structural decisions never retain a stale view of the board.

/// <summary>Owns typed, deterministic traversal of structural effect nodes.</summary>
internal static class AbilityStructuralFlowExecution
{
    internal static AbilityStructuralTransition AnswerGenericChoice(
        AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation, Decision answer) =>
        AbilityStructuralQueries.AnswerChoice(context, choice, continuation, answer);

    internal static AbilityStructuralTransition Choose(
        AbilityStructuralContext context, AbilityEffect.Choose choice)
    {
        if (choice.Options.Length < 2)
            return new Rejected($"'{context.SourceFace}' offers a choice of one, which is not a choice");
        if (choice.Options.Any(option => AbilityAdmission.IsOptionLegal(option, context.Admission())))
            return AskFor(context, choice);

        bool mandatoryEncounter = !AbilityCardQueries.IsPlayerCard(
            context.Expressions.World.Facts, context.Expressions.Source)
            && context.Tier is { } tier && AbilityTypes.IsMandatory(tier);
        return mandatoryEncounter ? new Complete(context.Frames) : new Rejected(
            $"'{context.SourceFace}' requires a choice and has no legal option");
    }

    internal static AbilityStructuralTransition ChooseCard(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice)
    {
        var candidates = AbilityAdmission.LegalCardChoices(choice, context.Admission());
        if (candidates.Count == 0) return new Complete(context.Frames);
        // rr:first-player.1 assigns an encounter selection only when there are
        // "multiple eligible targets". A sole target does not require an answer.
        if (choice.FirstPlayerChooses && context.IsMandatoryEncounter && candidates.Count == 1)
            return AnswerGenericChoice(context, choice, AbilityContinuationFacts.Empty,
                Decision.Take(candidates[0].ObjectId));
        return AskFor(context, choice);
    }

    internal static AbilityStructuralTransition EachPlayer(
        AbilityStructuralContext context, AbilityEffect.EachPlayer each)
    {
        if (AbilityResolutionAdmission.HasNestedEachPlayer(each, context.Admission()))
            return new Unsupported($"'{context.SourceFace}' nests one each-player frame inside another, which is not implemented");
        return new ScheduleEachPlayer(each, new EachPlayerFrame(context.Player, false));
    }

    internal static AbilityStructuralTransition ChooseTopForHand(
        AbilityStructuralContext context, AbilityEffect.ChooseTopForHand top) =>
        TopCards(context.Expressions.World.Seats[context.Player].Deck, top.Count).Count == 0
            ? new Complete(context.Frames) : AskFor(context, top);

    internal static AbilityStructuralTransition AfterActivation(
        AbilityStructuralContext context, AbilityEffect.AfterActivation after) =>
        context.Expressions.World.Activation is null
            ? new Unsupported($"'{context.SourceFace}' delays an effect and no enemy is activating")
            : new DelayAfterActivation(after);

    internal static Ask AskFor(AbilityStructuralContext context, AbilityEffect choice) =>
        new(choice, context.Frames.Add(new ChoiceFrame(null, null)));

    internal static AbilityStructuralTransition SequenceStart(
        AbilityStructuralContext context, AbilityEffect.Sequence sequence, int from)
    {
        var admission = from == 0
            ? AbilityEntryAdmission.AdmitStructure(sequence, context.Admission())
            : null;
        return NextSequence(context, sequence,
            new SequenceFrame(from, sequence.Effects.Length),
            new AbilityStructuralObservation(false), admission);
    }

    internal static AbilityStructuralTransition NextSequence(
        AbilityStructuralContext context, AbilityEffect.Sequence sequence,
        SequenceFrame frame, AbilityStructuralObservation observation,
        AbilityAdmissionResult? admission = null)
    {
        if (observation.Suspended)
            return new Complete(context.Frames.Add(frame), admission);
        frame = frame with { CompletedOutcome = AbilityCompletion.Combine(frame.CompletedOutcome, observation.Outcome) };
        if (frame.Next < 0 || frame.Count != sequence.Effects.Length || frame.Next > frame.Count)
            return new Rejected("invalid sequence cursor");
        if (frame.Next == frame.Count)
            return new Complete(context.Frames.Add(frame), admission, frame.CompletedOutcome);

        int position = frame.Next;
        var next = frame with { Next = position + 1 };
        return new RunLeaf(
            sequence.Effects[position], context.Frames.Add(next), position,
            context.HasContinuation || position < frame.Count - 1, admission);
    }

    internal static AbilityStructuralTransition NextSimultaneous(
        AbilityStructuralContext context, AbilityEffect.Simultaneous simultaneous,
        SimultaneousFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended)
            return new Complete(context.Frames.Add(frame));
        frame = frame with { CompletedOutcome = AbilityCompletion.Combine(frame.CompletedOutcome, observation.Outcome) };
        if (!ValidSimultaneousCursor(frame, simultaneous.Effects.Length))
            return new Rejected("invalid simultaneous cursor");
        if (frame.Remaining.IsEmpty)
            return new Complete(context.Frames.Add(frame), Outcome: frame.CompletedOutcome);

        int current = frame.Remaining[0];
        var next = new SimultaneousFrame(
            current, frame.Remaining.RemoveAt(0), frame.Completed.Add(frame.Current))
            { CompletedOutcome = frame.CompletedOutcome };
        return new RunLeaf(
            simultaneous.Effects[current], context.Frames.Add(next), context.Position,
            context.HasContinuation || next.Remaining.Length > 0);
    }

    private static bool ValidSimultaneousCursor(SimultaneousFrame frame, int count) =>
        frame.Current >= 0 && frame.Current < count
        && frame.Completed.Length + frame.Remaining.Length + 1 == count
        && frame.Remaining.All(index => index >= 0 && index < count)
        && frame.Completed.All(index => index >= 0 && index < count)
        && frame.Completed.Append(frame.Current).Concat(frame.Remaining).Distinct().Count()
            == count;

    internal static AbilityStructuralTransition Conditional(
        AbilityStructuralContext context, AbilityEffect.Conditional conditional)
    {
        bool then = Test(conditional.Test, context);
        AbilityEffect? selected = then ? conditional.Then : conditional.Else;
        var frames = context.Frames.Add(new ConditionalFrame(then));
        return selected is null
            ? new Complete(frames)
            : new RunLeaf(selected, frames, context.Position, context.HasContinuation);
    }

    internal static AbilityStructuralTransition Dependent(
        AbilityStructuralContext context, AbilityEffect.Dependent dependent)
    {
        AbilityCompletionSupport.Validate(dependent.Effect, context.SourceFace);
        return DependentLeaf(context, dependent, dependent.Effect,
            predecessor: true, outcome: null, hasContinuation: context.HasContinuation);
    }

    internal static AbilityStructuralTransition AfterDependentLeaf(
        AbilityStructuralContext context, AbilityEffect.Dependent dependent,
        DependentFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended) return new Complete(context.Frames);
        if (!frame.Predecessor)
            return new Complete(context.Frames, Outcome: AbilityCompletion.Combine(frame.Outcome, observation.Outcome));
        var required = dependent.OnFull
            ? AbilityStructuralOutcome.Full
            : AbilityStructuralOutcome.None;
        var outcome = observation.Outcome ?? throw new InvalidOperationException(
            "a dependent predecessor completed without a resolution result");
        return outcome == required
            ? DependentLeaf(context, dependent, dependent.Continuation,
                predecessor: false, outcome, context.HasContinuation)
            : new Complete(context.Frames, Outcome: outcome);
    }

    internal static AbilityStructuralTransition ForEachStart(
        AbilityStructuralContext context, AbilityEffect.ForEach repeated)
    {
        long count = AbilityEffectAdmissionConstraints.NonNegativeForEachCount(
            Amount(repeated.Count, context));
        if (count == 0)
            return new Complete(context.Frames);

        if (!AbilityChoiceAnalysis.Choices(repeated.Effect).Any())
        {
            if (repeated.Effect is AbilityEffect.Damage or AbilityEffect.RemoveThreat)
                return new RunCombinedForEach(repeated.Effect, count);
            if (AbilityInitiationPrimitives.ContainsForEachTarget(repeated.Effect))
                return new Unsupported(
                    $"'{context.SourceFace}' has a targeted for-each effect without choose "
                    + "whose one target cannot be persisted");
        }

        return NextForEach(context, repeated, new ForEachFrame(0, count),
            new AbilityStructuralObservation(false));
    }

    internal static AbilityStructuralTransition NextForEach(
        AbilityStructuralContext context, AbilityEffect.ForEach repeated,
        ForEachFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended) return new Complete(context.Frames.Add(frame));
        frame = frame with { CompletedOutcome = AbilityCompletion.Combine(frame.CompletedOutcome, observation.Outcome) };
        if (frame.Next >= frame.Count)
            return new Complete(context.Frames.Add(frame), Outcome: frame.CompletedOutcome);

        long iteration = frame.Next;
        var next = frame with { Next = iteration + 1 };
        return new RunLeaf(
            repeated.Effect, context.Frames.Add(next), context.Position,
            context.HasContinuation || iteration < frame.Count - 1);
    }

    internal static AbilityStructuralTransition EachTimeStart(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated, long from,
        long? persistedCount = null, ImmutableArray<int> receipt = default)
    {
        if (repeated.Effect is not AbilityEffect.DiscardTop
            { From: AbilitySearchArea.EncounterDeck, Players: null } discard)
            return new Unsupported(
                $"'{context.SourceFace}' uses each-time around an unsupported preceding effect");

        long requested = receipt.IsDefault ? persistedCount ?? Amount(discard.Count, context) : receipt.Length;
        if (requested < 0)
            return new Rejected("'eachTime' needs a non-negative discard count");
        if (requested == 0)
            return new Complete(context.Frames);

        long count = requested;
        if (persistedCount is null && receipt.IsDefault)
        {
            var deck = context.Expressions.World.AreaOf(DeckType.EncounterDeck);
            var pile = context.Expressions.World.AreaOf(DeckType.EncounterDiscardPile);
            long available = deck.Cards.Count > 0 ? deck.Cards.Count : pile.Cards.Count;
            count = Math.Min(requested, available);
        }
        return NextEachTime(context, repeated, new EachTimeFrame(from, count, null) { Receipt = receipt },
            new AbilityStructuralObservation(false));
    }

    internal static AbilityStructuralTransition NextEachTime(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated,
        EachTimeFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended) return new Complete(context.Frames.Add(frame));
        frame = frame with { CompletedOutcome = AbilityCompletion.Combine(frame.CompletedOutcome, observation.Outcome) };
        if (frame.Next >= frame.Count)
            return new Complete(context.Frames.Add(frame), Outcome: frame.CompletedOutcome);

        return new DiscardEachTime(
            new AbilityEffect.DiscardTop(
                AbilitySearchArea.EncounterDeck, Players: null,
                new AbilityNumber.Constant(1)), frame);
    }

    internal static AbilityStructuralTransition AfterEachTimeDiscard(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated,
        EachTimeFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Discarded is not { } discarded)
            return new Complete(context.Frames.Add(frame));

        var next = frame with { Next = frame.Next + 1, DiscardedCard = discarded.ObjectId };
        if (!Test(repeated.When, context))
            return NextEachTime(context, repeated, next,
                new AbilityStructuralObservation(false));

        return new RunLeaf(
            repeated.Then, context.Frames.Add(next), context.Position,
            context.HasContinuation || frame.Next < frame.Count - 1);
    }

    internal static RunLeaf DependentLeaf(
        AbilityStructuralContext context, AbilityEffect.Dependent dependent,
        AbilityEffect effect, bool predecessor, AbilityStructuralOutcome? outcome,
        bool hasContinuation)
    {
        var frame = new DependentFrame(dependent.OnFull, predecessor, outcome);
        return new RunLeaf(effect, context.Frames.Add(frame),
            context.Position, hasContinuation);
    }

    internal static long Amount(AbilityNumber number, AbilityStructuralContext context) => AbilityStructuralEvaluation.Amount(number, context);
    internal static bool Test(AbilityCondition condition, AbilityStructuralContext context) => AbilityStructuralEvaluation.Test(condition, context);
    internal static AbilityExpressionEvaluation Evaluation(AbilityStructuralContext context) => AbilityStructuralEvaluation.Evaluation(context);
    internal static IReadOnlyList<Card> Every(AbilityCardSelection selection, AbilityStructuralContext context) => AbilityStructuralEvaluation.Every(selection, context);
    internal static Card? Find(AbilityCardSelection selection, AbilityStructuralContext context) => AbilityStructuralEvaluation.Find(selection, context);
    internal static List<Card> TopCards(Area deck, long count) => AbilityStructuralEvaluation.TopCards(deck, count);
    internal static T Publish<T>(AbilityQueryResult<T> result, World world) => AbilityStructuralEvaluation.Publish(result, world);
}
