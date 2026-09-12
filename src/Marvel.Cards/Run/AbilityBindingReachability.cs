using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityDelayedReachability;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmissionMutation;
using static Marvel.Cards.Run.AbilityBindingReachability;
using static Marvel.Cards.Run.AbilityInitiationConstraints;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityBindingReachability
{
    internal static BindingCandidateState BindingCandidatesAfter(
        AbilityEffect node, AbilityAdmissionScope cast, BindingCandidateState before)
    {
        string operation = node.OperationName();
        if (operation is "attack" or "thwart")
        {
            // A labelled power owns `chosen` only inside its wrapper. Its
            // effect cannot suspend, and the outer binding resumes afterwards.
            return before;
        }
        if (operation == "chooseCard")
        {
            return ChooseCardBindingCandidatesAfter(node, cast, before);
        }
        if (operation == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (operation == "eachPlayer")
        {
            return EachPlayerBindingCandidatesAfter(node, cast, before);
        }
        if (operation == "if")
        {
            return ConditionalBindingCandidatesAfter(node, cast, before);
        }
        if (operation == "choose")
        {
            return ChoiceBindingCandidatesAfter(node, cast, before);
        }
        var candidates = before;
        foreach (var child in MutationChildren(node))
        {
            candidates = BindingCandidatesAfter(child, cast, candidates);
        }
        return candidates;
    }

    private static BindingCandidateState EachPlayerBindingCandidatesAfter(
        AbilityEffect node, AbilityAdmissionScope cast, BindingCandidateState before)
    {
        int original = cast.Player;
        try
        {
            var possible = new List<Card>();
            bool mayBeEmpty = false;
            foreach (var order in PlayerPermutations(cast.World.PlayerOrder.ToList()))
            {
                // Each scheduled frame restores the same persisted outer binding.
                // Only the final frame supplies the continuation's binding.
                cast.RestorePlayer(order[^1]);
                var finalFrame = BindingCandidatesAfter(EffectBody(node), cast, before);
                possible.AddRange(finalFrame.Cards);
                mayBeEmpty |= finalFrame.MayBeEmpty;
            }
            return new BindingCandidateState(
                possible.DistinctBy(card => card.ObjectId).ToList(), mayBeEmpty);
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    private static BindingCandidateState ConditionalBindingCandidatesAfter(
        AbilityEffect node, AbilityAdmissionScope cast, BindingCandidateState before)
    {
        var test = ConditionalOf(node, cast).Test;
        var branches = ConditionalCanSwitch(test, cast, before)
            ? ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(value => value is not null)
            : ActiveConditionalBranch(node, test, cast);
        var outcomes = branches.Select(branch => BindingCandidatesAfter(
                branch, cast, before))
            .ToList();
        return new BindingCandidateState(
            outcomes.SelectMany(outcome => outcome.Cards)
                .DistinctBy(card => card.ObjectId).ToList(),
            outcomes.Count == 0 || outcomes.Any(outcome => outcome.MayBeEmpty));
    }

    private static bool ConditionalCanSwitch(
        AbilityCondition test, AbilityAdmissionScope cast,
        BindingCandidateState before) =>
        before.Cards.Count > 0 && BindingCanChange(test)
        || PriorStepCanChange(test, cast)
        || cast.Reachability.PaymentMayMutate && PaymentCanChange(test);

    private static IEnumerable<AbilityEffect> ActiveConditionalBranch(
        AbilityEffect node, AbilityCondition test, AbilityAdmissionScope cast) =>
        ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
            ? [active]
            : [];

    internal static BindingCandidateState ChooseCardBindingCandidatesAfter(
        AbilityEffect node, AbilityAdmissionScope cast, BindingCandidateState before)
    {
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var possible = new List<Card>();
        bool mayBeEmpty = false;
        try
        {
            void AddOutcome(Card? binding)
            {
                cast.ChooseSelection(binding);
                var legal = LegalCardChoices(node, cast);
                if (legal.Count > 0)
                {
                    foreach (var chosen in legal)
                    {
                        cast.ChooseSelection(chosen);
                        var afterEffect = BindingCandidatesAfter(
                            EffectBody(node), cast,
                            new BindingCandidateState([chosen], MayBeEmpty: false));
                        possible.AddRange(afterEffect.Cards);
                        mayBeEmpty |= afterEffect.MayBeEmpty;
                    }
                }
                else if (binding is not null)
                {
                    // Runtime choice resolution is a no-op when this frame has
                    // no legal target, so an earlier binding survives it.
                    possible.Add(binding);
                }
                else
                {
                    mayBeEmpty = true;
                }
            }

            foreach (var candidate in before.Cards)
            {
                AddOutcome(candidate);
            }
            if (before.MayBeEmpty)
            {
                AddOutcome(null);
            }
            if (before.Cards.Count == 0 && !before.MayBeEmpty)
            {
                AddOutcome(prior?.Card);
            }
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
        return new BindingCandidateState(
            possible.DistinctBy(card => card.ObjectId).ToList(), mayBeEmpty);
    }

    internal static BindingCandidateState ChoiceBindingCandidatesAfter(
        AbilityEffect node, AbilityAdmissionScope cast, BindingCandidateState before,
        IReadOnlyList<AbilityEffect>? continuation = null)
    {
        var prior = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        var outcomes = new List<BindingCandidateState>();
        try
        {
            void AddOutcomes(Card? binding)
            {
                cast.ChooseSelection(binding);
                var incoming = new BindingCandidateState(
                    binding is null ? [] : [binding], binding is null);
                foreach (var option in ((AbilityEffect.Choose)node).Options
                    .Where(option => OptionIsLegal(option, cast)))
                {
                    var outcome = BindingCandidatesAfter(option, cast, incoming);
                    outcomes.Add(continuation is null
                        ? outcome
                        : FilterCandidatesForContinuation(
                            outcome, continuation, cast));
                }
            }

            foreach (var candidate in before.Cards)
            {
                AddOutcomes(candidate);
            }
            if (before.MayBeEmpty)
            {
                AddOutcomes(null);
            }
            if (before.Cards.Count == 0 && !before.MayBeEmpty)
            {
                AddOutcomes(prior?.Card);
            }
        }
        finally
        {
            cast.RestoreChosen(prior);
            cast.RestorePlayerSelection(priorSelection);
        }
        return new BindingCandidateState(
            outcomes.SelectMany(outcome => outcome.Cards)
                .DistinctBy(card => card.ObjectId).ToList(),
            outcomes.Any(outcome => outcome.MayBeEmpty));
    }

    internal static BindingCandidateState FilterCandidatesForContinuation(
        BindingCandidateState candidates,
        IReadOnlyList<AbilityEffect> continuation,
        AbilityAdmissionScope cast)
    {
        var suffix = new AbilityEffect.Sequence([.. continuation]);
        var legal = candidates.Cards.Where(candidate =>
        {
            var scope = cast.ForReachability(cast.Reachability with
            {
                PriorBindingCandidates = [candidate],
                PriorBindingMayBeEmpty = false,
                PriorBindingMayChange = false,
            });
            scope.ChooseSelection(candidate);
            return CanInitiateSequence(suffix, scope)
                && TargetLegalityOf(suffix, scope) != TargetLegality.Invalid;
        }).ToList();
        // An explicit empty option is the authored decline branch for “may.”
        // It remains reachable; the enclosing sequence rejects it if its
        // suffix requires a binding. Card-bearing alternatives filter separately.
        return new BindingCandidateState(legal, candidates.MayBeEmpty);
    }

    internal static void PreflightContinuationBoundaries(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node.OperationName() == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.OperationName() == "seq")
        {
            var steps = OrderedEffects(node).ToList();
            bool outerContinuation = cast.HasContinuation;
            try
            {
                for (int step = 0; step < steps.Count; step++)
                {
                    cast.SetContinuation(outerContinuation || step < steps.Count - 1);
                    PreflightContinuationBoundaries(steps[step], cast);
                }
            }
            finally
            {
                cast.SetContinuation(outerContinuation);
            }
            return;
        }

        if (node.OperationName() == "and")
        {
            _ = CanInitiateAnd(node, cast);
            return;
        }

        var children = node.OperationName() switch
        {
            "choose" => ((AbilityEffect.Choose)node).Options,
            "eachPlayer" => [EffectBody(node)],
            _ => ResolutionChildren(node),
        };
        foreach (var child in children)
        {
            PreflightContinuationBoundaries(child, cast);
        }
    }

}
