using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityCostSelection;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static bool CanPartiallyResolve(AbilityEffect node, Cast cast) =>
        AbilityInitiation.CanPartiallyResolve(node, AdmissionContext(cast));

    private static ResolutionOutcome ResolutionOf(AbilityEffect node, Cast cast) =>
        (ResolutionOutcome)(int)AbilityInitiation.ResolutionOf(node, AdmissionContext(cast));

    private static ResolutionOutcome EnsureDependentSupported(
        AbilityEffect node, Cast cast, AbilityEffect effect,
        AbilityEffect dependent, ResolutionOutcome required) =>
        (ResolutionOutcome)(int)AbilityInitiation.EnsureDependentSupported(
            node, AdmissionContext(cast), effect, dependent,
            (AbilityInitiation.ResolutionOutcome)(int)required);

    private static void PreflightAnsweredOutcome(AbilityEffect node, Cast cast) =>
        AbilityInitiation.PreflightAnsweredOutcome(node, AdmissionContext(cast));

    private static void PreflightResolutionBranches(
        AbilityEffect node, Cast cast, bool allBranches = false) =>
        AbilityInitiation.PreflightResolutionBranches(
            node, AdmissionContext(cast), allBranches);

    private static bool PaymentCanChange(AbilityCondition test) =>
        AbilityInitiation.PaymentCanChange(test);

    private static bool ContainsNode(AbilityEffect node, string kind, Cast cast) =>
        AbilityInitiation.ContainsNode(node, kind, AdmissionContext(cast));

    private static bool HasNestedEachPlayer(
        AbilityEffect node, Cast cast, bool inside = false,
        bool stateMayChange = false, bool bindingMayChange = false,
        AbilityEffect? repeatedEffect = null) =>
        AbilityInitiation.HasNestedEachPlayer(
            node, AdmissionContext(cast), inside, stateMayChange,
            bindingMayChange, repeatedEffect);

    private static void ResolveDependent(
        AbilityEffect node, Cast cast, ResolutionOutcome required, string branch)
    {
        var effect = EffectBody(node);
        var dependent = ContinuationChild(node, branch);
        if (ActiveChoices(effect, cast).Any())
        {
            PreflightAnsweredOutcome(effect, cast);
            PreflightContinuationBoundaries(dependent, cast);
            RunChild(effect, $"{node.OperationName()}:effect:Pending", cast);
            return;
        }
        var outcome = EnsureDependentSupported(node, cast, effect, dependent, required);

        // A supported predecessor classified as `None` changes no state. Some
        // low-level resolvers deliberately reject a missing target when used
        // alone; dependency words make that absence an expected outcome, so
        // do not turn an advertised `otherwise` fallback into an exception.
        if (outcome != ResolutionOutcome.None)
        {
            RunChild(effect, $"{node.OperationName()}:effect:{outcome}", cast);
        }
        if (outcome == required)
        {
            RunChild(dependent, $"{node.OperationName()}:{branch}", cast);
        }
    }


}
