using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityChoicePromptDescription;

namespace Marvel.Cards.Run;

/// <summary>Choice legality over concrete immutable expression and suffix facts.</summary>
internal static class AbilityStructuralQueries
{
    internal static AbilityStructuralTransition AnswerChoice(
        AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation, Decision answer)
    {
        var evidence = NewEvidence();
        if (choice is AbilityEffect.ChooseCard chooseCard)
            return AnswerCardChoice(context, chooseCard, continuation, answer, evidence);
        var options = (AbilityEffect.Choose)choice;
        if (answer.IsDecline || answer.Affordance < 0
            || answer.Affordance >= options.Options.Length)
        {
            return new Unsupported(
                $"'{context.SourceFace}' offers {options.Options.Length} options and none "
                + $"of them is number {answer.Affordance}");
        }

        var selectedOption = options.Options[answer.Affordance];
        bool requiresChange = options.Options.Any(IsExplicitDecline);
        if (!OptionIsLegal(
            context, selectedOption, continuation, requiresChange, evidence))
        {
            return new Unsupported(
                $"'{context.SourceFace}' cannot choose illegal option {answer.Affordance}");
        }

        AbilityStructuralOutcome? optionOutcome = context.HasPendingDependency
            ? Outcome(AbilityResolutionAdmission.ResolutionOf(
                selectedOption, context.Admission()))
            : null;
        return new RunChoice(
            selectedOption, new ChoiceFrame(answer.Affordance, null), null,
            BindsPlayerSelection: false, optionOutcome, Admission(evidence));
    }

    private static AbilityStructuralTransition AnswerCardChoice(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice,
        AbilityContinuationFacts continuation, Decision answer,
        HashSet<AbilityEffect> evidence)
    {
        var selected = LegalCards(context, choice, continuation, evidence)
            .FirstOrDefault(card => card.ObjectId == answer.Affordance);
        if (selected is null)
            return new Unsupported(
                $"'{context.SourceFace}' did not offer card {answer.Affordance} to choose");
        var selectedContext = WithSelection(context, selected);
        AbilityStructuralOutcome? pending = null;
        if (context.HasPendingDependency
            && !AbilityChoiceAnalysis.ActiveChoices(choice.Effect, selectedContext.Admission()).Any())
        {
            pending = Outcome(AbilityResolutionAdmission.ResolutionOf(
                choice.Effect, selectedContext.Admission()));
        }
        return new RunChoice(
            choice.Effect, new ChoiceFrame(null, selected.ObjectId), selected,
            BindsPlayerSelection: true, pending, Admission(evidence));
    }

    /// <summary>Whether one listed option may be chosen and leaves its suffix resolvable.</summary>
    /// <remarks>
    /// <c>rr:choose-option.1</c> requires an encounter-card option's targets to be
    /// valid. <c>rr:choose-option.2</c> requires a player-card option to change the
    /// game at least partially; an empty sequence is the explicit decline branch.
    /// </remarks>
    internal static bool OptionIsLegal(
        AbilityStructuralContext context, AbilityEffect option,
        AbilityContinuationFacts continuation, bool requireStateChange,
        HashSet<AbilityEffect> evidence)
    {
        var admission = context.Admission();
        bool local = AbilityAdmission.IsOptionLegal(option, admission)
            && (!requireStateChange || IsExplicitDecline(option)
                || AbilityResolutionAdmission.CanPartiallyResolve(option, admission));
        if (!local || !continuation.HasPath)
            return local;

        var prior = admission.Query.ChosenBinding;
        AbilityAdmission.AdmissionResolution? pending = context.HasPendingDependency
            ? AbilityResolutionAdmission.ResolutionOf(option, admission)
            : null;
        var outcomes = AbilityAdmission.BindingCandidates(
            option, admission,
            new BindingCandidateState(
                prior is null ? [] : [prior.Card], prior is null));
        var after = admission.WithReachability(admission.Reachability with
        {
            PriorSteps = admission.Reachability.PriorSteps.Add(option),
            FilteringContinuationOption = true,
        });
        return ContinuationCanResolve(
            context, continuation, outcomes, after, pending, evidence);
    }

    /// <summary>Targets meeting both the selector, nested effect, and remaining suffix.</summary>
    /// <remarks>
    /// <c>rr:target.2.2</c> makes choose-card a target selection, so each candidate
    /// is bound before the nested effect and structural continuation are admitted.
    /// </remarks>
    internal static List<Card> LegalCards(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice,
        AbilityContinuationFacts continuation, HashSet<AbilityEffect> evidence)
    {
        var legal = AbilityAdmission.LegalCardChoices(choice, context.Admission());
        if (!continuation.HasPath)
            return legal;

        return legal.Where(candidate =>
        {
            var selected = context.Admission().WithSelection(candidate);
            AbilityAdmission.AdmissionResolution? pending = context.HasPendingDependency
                && !AbilityChoiceAnalysis.ActiveChoices(choice.Effect, selected).Any()
                    ? AbilityResolutionAdmission.ResolutionOf(choice.Effect, selected)
                    : null;
            var outcomes = AbilityAdmission.BindingCandidates(
                choice.Effect, selected,
                new BindingCandidateState([candidate], false));
            var after = selected.WithReachability(selected.Reachability with
            {
                PriorSteps = selected.Reachability.PriorSteps.Add(choice.Effect),
                FilteringContinuationOption = true,
            });
            return ContinuationCanResolve(
                context, continuation, outcomes, after, pending, evidence);
        }).ToList();
    }

    private static bool ContinuationCanResolve(
        AbilityStructuralContext context, AbilityContinuationFacts continuation,
        BindingCandidateState outcomes,
        AbilityAdmissionContext admission,
        AbilityAdmission.AdmissionResolution? pending,
        HashSet<AbilityEffect> evidence)
    {
        bool CanResolve(Card? binding)
        {
            var selected = admission.WithReachability(admission.Reachability with
            {
                CheckingInitiation = true,
                PriorBindingCandidates = binding is null ? [] : [binding],
                PriorBindingMayBeEmpty = binding is null,
                PriorBindingMayChange = false,
            }).WithSelection(binding);
            var remaining = Remaining(context, continuation, selected, pending, evidence);
            if (remaining.Count == 0)
                return true;

            var sensitive = new HashSet<DeckType>();
            foreach (var step in remaining)
                AbilityAdmissionAreaDependencies.Collect(step, selected, sensitive);
            if (sensitive.Count > 0
                && AbilityRuntimeQueries.EffectsMayChangeAnyArea(
                    selected.Reachability.PriorSteps, sensitive, selected))
            {
                return false;
            }

            var sequence = new AbilityEffect.Sequence([.. remaining]);
            var admitted = AbilityAdmission.AdmitStructure(sequence, selected);
            AddEvidence(evidence, admitted);
            return admitted.IsAdmissible
                && AbilityAdmission.TargetsAreValid(sequence, selected);
        }

        return outcomes.Cards.Any(CanResolve)
            || outcomes.MayBeEmpty && CanResolve(null);
    }

    private static List<AbilityEffect> Remaining(
        AbilityStructuralContext context, AbilityContinuationFacts continuation,
        AbilityAdmissionContext admission,
        AbilityAdmission.AdmissionResolution? pending,
        HashSet<AbilityEffect> evidence)
    {
        var remaining = new List<AbilityEffect>();
        for (int position = continuation.Frames.Length - 1; position >= 0; position--)
        {
            var frame = continuation.Frames[position];
            if (frame is EachPlayerContinuationFrame { StopsOuterContinuation: true })
                return remaining;
            if (frame is EachTimeContinuationFrame repeated
                && repeated.Current + 1 < repeated.Count
                && LaterEachTimePromptIsGuaranteed(
                    context, repeated, admission, evidence))
                return remaining;
            AppendRemainingFrame(frame, remaining, pending);
        }
        return remaining;
    }

    private static void AppendRemainingFrame(
        AbilityContinuationFrame frame, List<AbilityEffect> remaining,
        AbilityAdmission.AdmissionResolution? pending)
    {
        switch (frame)
        {
            case SequenceContinuationFrame sequence:
                remaining.AddRange(sequence.Parent.Effects.Skip(sequence.Current + 1));
                break;
            case DependentContinuationFrame dependent when dependent.Predecessor:
                AppendDependentContinuation(dependent, remaining, pending);
                break;
            case SimultaneousContinuationFrame simultaneous:
                remaining.AddRange(simultaneous.Remaining.Select(
                    index => simultaneous.Parent.Effects[index]));
                break;
            case ForEachContinuationFrame repeated:
                for (long next = repeated.Current + 1; next < repeated.Count; next++)
                    remaining.Add(repeated.Parent.Effect);
                break;
        }
    }

    private static void AppendDependentContinuation(
        DependentContinuationFrame dependent, List<AbilityEffect> remaining,
        AbilityAdmission.AdmissionResolution? pending)
    {
        AbilityStructuralOutcome? outcome = dependent.Outcome
            ?? (pending is { } recorded ? Outcome(recorded) : null);
        var required = dependent.Parent.OnFull
            ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None;
        if (outcome == required) remaining.Add(dependent.Parent.Continuation);
    }

    private static bool LaterEachTimePromptIsGuaranteed(
        AbilityStructuralContext context, EachTimeContinuationFrame repeated,
        AbilityAdmissionContext admission, HashSet<AbilityEffect> evidence)
    {
        long count = repeated.Count - repeated.Current - 1;
        var future = context.Expressions.World.AreaOf(DeckType.EncounterDeck).Cards
            .Reverse().Take((int)Math.Min(count, int.MaxValue)).ToList();
        if (future.Count < count)
            return false;

        foreach (var card in future)
        {
            var altered = admission.WithAltered(card);
            if (Test(repeated.Parent.When, altered.Expressions)
                && AbilityChoiceAnalysis.ActiveChoices(repeated.Parent.Then, altered).Any())
            {
                var admitted = AbilityAdmission.Admit(repeated.Parent.Then, altered);
                AddEvidence(evidence, admitted);
                if (admitted.IsAdmissible)
                    return true;
            }
        }
        return false;
    }

    private static AbilityStructuralContext WithSelection(
        AbilityStructuralContext context, Card card)
    {
        var admission = context.Admission().WithSelection(card);
        return context with { Expressions = admission.Expressions };
    }

    internal static long Amount(AbilityNumber number, AbilityExpressionContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Amount(number)), context.World);
    }

    internal static bool Test(AbilityCondition condition, AbilityExpressionContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Test(condition)), context.World);
    }

    private static AbilityExpressionEvaluation Evaluation(AbilityExpressionContext context) =>
        new(context, new AbilitySelectorEvaluation(context.Bindings));

    internal static HashSet<AbilityEffect> NewEvidence() =>
        new(ReferenceEqualityComparer.Instance);

    private static void AddEvidence(
        HashSet<AbilityEffect> evidence, AbilityAdmissionResult admitted) =>
        evidence.UnionWith(admitted.CrisisIgnoringThwarts);

    internal static AbilityAdmissionResult Admission(HashSet<AbilityEffect> evidence) =>
        new(true, ImmutableHashSet.CreateRange<AbilityEffect>(
            ReferenceEqualityComparer.Instance, evidence));

    private static AbilityStructuralOutcome Outcome(
        AbilityAdmission.AdmissionResolution outcome) =>
        (AbilityStructuralOutcome)(int)outcome;

    private static T Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information)
            world.RecordInformation(observation);
        return result.Value;
    }
}
