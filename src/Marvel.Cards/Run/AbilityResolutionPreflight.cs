using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityCostSelection;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionPreflight
{
    internal static bool CanPartiallyResolve(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast) =>
        AbilityResolutionAdmission.CanPartiallyResolve(node, execution.AdmissionContext(cast));

    internal static ResolutionOutcome ResolutionOf(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast) =>
        (ResolutionOutcome)(int)AbilityResolutionAdmission.ResolutionOf(node, execution.AdmissionContext(cast));

    internal static ResolutionOutcome EnsureDependentSupported(this AbilityResolutionExecution execution,
        AbilityEffect node, AbilityResolutionState cast, AbilityEffect effect,
        AbilityEffect dependent, ResolutionOutcome required) =>
        (ResolutionOutcome)(int)AbilityResolutionAdmission.EnsureDependentSupported(
            node, execution.AdmissionContext(cast), effect, dependent,
            (AbilityAdmission.AdmissionResolution)(int)required);

    internal static void PreflightAnsweredOutcome(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast) =>
        AbilityResolutionAdmission.PreflightAnsweredOutcome(node, execution.AdmissionContext(cast));

    internal static void PreflightResolutionBranches(this AbilityResolutionExecution execution,
        AbilityEffect node, AbilityResolutionState cast, bool allBranches = false) =>
        AbilityResolutionAdmission.PreflightResolutionBranches(
            node, execution.AdmissionContext(cast), allBranches);

    internal static bool PaymentCanChange(this AbilityResolutionExecution execution, AbilityCondition test) =>
        AbilityAdmissionResolutionPreflight.PaymentCanChange(test);

    internal static bool ContainsNode(this AbilityResolutionExecution execution, AbilityEffect node, string kind, AbilityResolutionState cast) =>
        AbilityResolutionAdmission.ContainsNode(node, kind, execution.AdmissionContext(cast));

    internal static bool HasNestedEachPlayer(this AbilityResolutionExecution execution,
        AbilityEffect node, AbilityResolutionState cast, bool inside = false,
        bool stateMayChange = false, bool bindingMayChange = false,
        AbilityEffect? repeatedEffect = null) =>
        AbilityResolutionAdmission.HasNestedEachPlayer(
            node, execution.AdmissionContext(cast), inside, stateMayChange,
            bindingMayChange, repeatedEffect);

    internal static void ResolveDependent(this AbilityResolutionExecution execution,
        AbilityEffect.Dependent dependent, AbilityResolutionState cast)
    {
        bool outerContinuation = cast.HasContinuation;
        var transition = AbilityStructuralFlowExecution.Dependent(
            execution.StructuralContext(cast), dependent);
        while (transition is RunLeaf leaf
            && leaf.Frames[^1] is DependentFrame frame)
        {
            execution.RunStructuralLeaf(leaf, cast);
            var observation = new AbilityStructuralObservation(cast.Suspended);
            if (!cast.Suspended)
                cast.SetContinuation(outerContinuation);
            transition = AbilityStructuralFlowExecution.AfterDependentLeaf(
                execution.StructuralContext(cast), dependent, frame,
                observation);
            if (cast.Suspended)
                return;
        }
        cast.SetContinuation(outerContinuation);
    }


}
