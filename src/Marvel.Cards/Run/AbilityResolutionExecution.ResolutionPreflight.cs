using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityCostSelection;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private bool CanPartiallyResolve(AbilityEffect node, AbilityResolutionState cast) =>
        AbilityInitiation.CanPartiallyResolve(node, AdmissionContext(cast));

    private ResolutionOutcome ResolutionOf(AbilityEffect node, AbilityResolutionState cast) =>
        (ResolutionOutcome)(int)AbilityInitiation.ResolutionOf(node, AdmissionContext(cast));

    private ResolutionOutcome EnsureDependentSupported(
        AbilityEffect node, AbilityResolutionState cast, AbilityEffect effect,
        AbilityEffect dependent, ResolutionOutcome required) =>
        (ResolutionOutcome)(int)AbilityInitiation.EnsureDependentSupported(
            node, AdmissionContext(cast), effect, dependent,
            (AbilityInitiation.ResolutionOutcome)(int)required);

    private void PreflightAnsweredOutcome(AbilityEffect node, AbilityResolutionState cast) =>
        AbilityInitiation.PreflightAnsweredOutcome(node, AdmissionContext(cast));

    private void PreflightResolutionBranches(
        AbilityEffect node, AbilityResolutionState cast, bool allBranches = false) =>
        AbilityInitiation.PreflightResolutionBranches(
            node, AdmissionContext(cast), allBranches);

    private static bool PaymentCanChange(AbilityCondition test) =>
        AbilityInitiation.PaymentCanChange(test);

    private bool ContainsNode(AbilityEffect node, string kind, AbilityResolutionState cast) =>
        AbilityInitiation.ContainsNode(node, kind, AdmissionContext(cast));

    private bool HasNestedEachPlayer(
        AbilityEffect node, AbilityResolutionState cast, bool inside = false,
        bool stateMayChange = false, bool bindingMayChange = false,
        AbilityEffect? repeatedEffect = null) =>
        AbilityInitiation.HasNestedEachPlayer(
            node, AdmissionContext(cast), inside, stateMayChange,
            bindingMayChange, repeatedEffect);

    private void ResolveDependent(
        AbilityEffect.Dependent dependent, AbilityResolutionState cast)
    {
        bool outerContinuation = cast.HasContinuation;
        var transition = AbilityStructuralExecution.Dependent(
            StructuralContext(cast), dependent);
        while (transition is RunLeaf leaf
            && leaf.Frames[^1] is DependentFrame frame)
        {
            RunStructuralLeaf(leaf, cast);
            var observation = new AbilityStructuralObservation(cast.Suspended);
            if (!cast.Suspended)
                cast.SetContinuation(outerContinuation);
            transition = AbilityStructuralExecution.AfterDependentLeaf(
                StructuralContext(cast), dependent, frame,
                observation);
            if (cast.Suspended)
                return;
        }
        cast.SetContinuation(outerContinuation);
    }


}
