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
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice) =>
        AbilityAdmission.LegalCardChoices(choice, context.Admission()).Count == 0
            ? new Complete(context.Frames)
            : AskFor(context, choice);

    internal static AbilityStructuralTransition EachPlayer(
        AbilityStructuralContext context, AbilityEffect.EachPlayer each)
    {
        if (AbilityResolutionAdmission.HasNestedEachPlayer(each, context.Admission()))
            return new Unsupported($"'{context.SourceFace}' nests one each-player frame inside another, which is not implemented");
        return new ScheduleEachPlayer(each, new EachPlayerFrame(context.Player, false));
    }

    internal static AbilityStructuralTransition ResolveSpecials(
        AbilityStructuralContext context, AbilityEffect.CardAction specials) =>
        Every(specials.Selection, context).Count == 0 ? new Complete(context.Frames) : AskFor(context, specials);

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
            ? AbilityAdmission.AdmitStructure(sequence, context.Admission())
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
        if (frame.Next < 0 || frame.Count != sequence.Effects.Length || frame.Next > frame.Count)
            return new Rejected("invalid sequence cursor");
        if (frame.Next == frame.Count)
            return new Complete(context.Frames.Add(frame), admission);

        int position = frame.Next;
        var next = new SequenceFrame(position + 1, frame.Count);
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
        if (!ValidSimultaneousCursor(frame, simultaneous.Effects.Length))
            return new Rejected("invalid simultaneous cursor");
        if (frame.Remaining.IsEmpty)
            return new Complete(context.Frames.Add(frame));

        int current = frame.Remaining[0];
        var next = new SimultaneousFrame(
            current, frame.Remaining.RemoveAt(0), frame.Completed.Add(frame.Current));
        return new RunLeaf(
            simultaneous.Effects[current], context.Frames.Add(next), context.Position,
            context.HasContinuation || next.Remaining.Length > 0);
    }

    private static bool ValidSimultaneousCursor(SimultaneousFrame frame, int count) =>
        frame.Current >= 0 && frame.Current < count
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
        var admission = context.Admission();
        var effect = dependent.Effect;
        if (AbilityChoiceAnalysis.ActiveChoices(effect, admission).Any())
        {
            AbilityResolutionAdmission.PreflightAnsweredOutcome(effect, admission);
            AbilityAdmission.PreflightContinuationBoundaries(dependent.Continuation, admission);
            return DependentLeaf(context, dependent, effect,
                predecessor: true, outcome: null,
                hasContinuation: context.HasContinuation);
        }

        var required = dependent.OnFull
            ? AbilityAdmission.AdmissionResolution.Full
            : AbilityAdmission.AdmissionResolution.None;
        var outcome = AbilityResolutionAdmission.EnsureDependentSupported(
            dependent, admission, effect, dependent.Continuation, required);
        var structural = (AbilityStructuralOutcome)(int)outcome;
        if (outcome == AbilityAdmission.AdmissionResolution.None)
        {
            return outcome == required
                ? DependentLeaf(context, dependent, dependent.Continuation,
                    predecessor: false, structural, context.HasContinuation)
                : new Complete(context.Frames);
        }

        return DependentLeaf(context, dependent, effect,
            predecessor: true, structural,
            hasContinuation: context.HasContinuation);
    }

    internal static AbilityStructuralTransition AfterDependentLeaf(
        AbilityStructuralContext context, AbilityEffect.Dependent dependent,
        DependentFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended || !frame.Predecessor)
            return new Complete(context.Frames);
        var required = dependent.OnFull
            ? AbilityStructuralOutcome.Full
            : AbilityStructuralOutcome.None;
        return frame.Outcome == required
            ? DependentLeaf(context, dependent, dependent.Continuation,
                predecessor: false, frame.Outcome, context.HasContinuation)
            : new Complete(context.Frames);
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
        if (observation.Suspended || frame.Next >= frame.Count)
            return new Complete(context.Frames.Add(frame));

        long iteration = frame.Next;
        var next = new ForEachFrame(iteration + 1, frame.Count);
        return new RunLeaf(
            repeated.Effect, context.Frames.Add(next), context.Position,
            context.HasContinuation || iteration < frame.Count - 1);
    }

    internal static AbilityStructuralTransition EachTimeStart(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated, long from,
        long? persistedCount = null)
    {
        if (repeated.Effect is not AbilityEffect.DiscardTop
            { From: AbilitySearchArea.EncounterDeck, Players: null } discard)
            return new Unsupported(
                $"'{context.SourceFace}' uses each-time around an unsupported preceding effect");

        long requested = persistedCount ?? Amount(discard.Count, context);
        if (requested < 0)
            return new Rejected("'eachTime' needs a non-negative discard count");
        if (requested == 0)
            return new Complete(context.Frames);
        if (persistedCount is null)
            AbilityDelayedReachability.ValidateEachTimeBody(repeated, context.Admission());

        long count = requested;
        if (persistedCount is null)
        {
            var deck = context.Expressions.World.AreaOf(DeckType.EncounterDeck);
            var pile = context.Expressions.World.AreaOf(DeckType.EncounterDiscardPile);
            long available = deck.Cards.Count > 0 ? deck.Cards.Count : pile.Cards.Count;
            count = Math.Min(requested, available);
        }
        return NextEachTime(context, repeated, new EachTimeFrame(from, count, null),
            new AbilityStructuralObservation(false));
    }

    internal static AbilityStructuralTransition NextEachTime(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated,
        EachTimeFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended || frame.Next >= frame.Count)
            return new Complete(context.Frames.Add(frame));

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

        var next = new EachTimeFrame(frame.Next + 1, frame.Count, discarded.ObjectId);
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

    internal static long Amount(AbilityNumber number, AbilityStructuralContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Amount(number)), context.Expressions.World);
    }

    internal static bool Test(AbilityCondition condition, AbilityStructuralContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Test(condition)), context.Expressions.World);
    }

    internal static AbilityExpressionEvaluation Evaluation(AbilityStructuralContext context) =>
        new(context.Expressions, new AbilitySelectorEvaluation(context.Expressions.Bindings));

    internal static IReadOnlyList<Card> Every(AbilityCardSelection selection, AbilityStructuralContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, program: context.Program);
        return Publish(evaluation.Result(evaluation.Every(selection)), context.Expressions.World);
    }

    internal static Card? Find(
        AbilityCardSelection selection, AbilityStructuralContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, program: context.Program);
        return Publish(evaluation.Result(evaluation.Find(selection)), context.Expressions.World);
    }

    internal static List<Card> TopCards(Area deck, long count) =>
        [.. deck.Cards.TakeLast(checked((int)Math.Max(0, count))).Reverse()];

    internal static T Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information)
            world.RecordInformation(observation);
        return result.Value;
    }
}
