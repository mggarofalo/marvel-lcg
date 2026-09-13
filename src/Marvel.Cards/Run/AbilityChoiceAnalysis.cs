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

namespace Marvel.Cards.Run;

internal static class AbilityChoiceAnalysis
{
    internal static IEnumerable<AbilityEffect> ActiveChoices(
        AbilityEffect node, AbilityAdmissionContext context) =>
        ActiveChoices(node, new AbilityAdmissionScope(context, []));

    internal static bool SuspendsInsideAnd(
        AbilityEffect node, AbilityAdmissionContext context,
        bool stateMayChange = false, bool bindingMayChange = false) =>
        SuspendsInsideAnd(node, new AbilityAdmissionScope(context, []),
            stateMayChange, bindingMayChange);

    /// <summary>Every <c>choose</c> node in one effect tree.</summary>
    internal static IEnumerable<AbilityEffect> Choices(AbilityEffect node)
    {
        if ((node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any())
            || IsChoice(node))
        {
            yield return node;
            yield break;
        }

        foreach (var found in ChoiceChildren(node).SelectMany(Choices))
        {
            yield return found;
        }
    }

    private static IEnumerable<AbilityEffect> ChoiceChildren(AbilityEffect node) =>
        node.OperationName() switch
        {
            "seq" or "and" => OrderedEffects(node),
            "if" => ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(branch => branch is not null)
                .Select(branch => branch),
            "then" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "otherwise" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "eachPlayer" or "forEach" => [EffectBody(node)],
            "defense" => [EffectBody(node)],
            _ => [],
        };

    internal static bool IsChoice(AbilityEffect node) =>
        node.OperationName() is "choose" or "chooseCard" or "indirectDamage"
            or "resolveSpecials" or "payOrExhaust" or "chooseTopForHand"
            or "chooseDiscardToShuffle" or "thwartDifferentSchemes" or "makeTheCall"
            or "legalPractice" or "payOrEffect" or "enemyAttacks" or "enemySchemes";

    /// <summary>Choice nodes on the control-flow path that can execute now.</summary>
    internal static IEnumerable<AbilityEffect> ActiveChoices(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (CurrentlyZeroForEach(node, cast)) return [];
        if (node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any())
            return [node];
        if (node.OperationName() is "enemyAttacks" or "enemySchemes")
            return ActivationCandidates(ActivationOf(node, cast), cast).Count > 1
                ? [node] : [];
        if (IsChoice(node))
            return DirectChoiceIsActive(node, cast) ? [node] : [];
        if (node.OperationName() is "then" or "otherwise")
            return ActiveDependentChoices(node, cast);

        return ActiveExecutionChildren(node, cast)
            .SelectMany(child => ActiveChoices(child, cast));
    }

    private static ImmutableArray<AbilityEffect> ActiveExecutionChildren(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
            "seq" or "and" => OrderedEffects(node),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch ? [branch] : [],
            "eachPlayer" or "forEach" => [EffectBody(node)],
            "defense" => [EffectBody(node)],
            _ => [],
        };
    private static bool DirectChoiceIsActive(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() != "indirectDamage"
        || Assignable(((AbilityEffect.IndirectDamage)node).Among, cast).Count > 1;

    private static IEnumerable<AbilityEffect> ActiveDependentChoices(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        var preceding = EffectBody(node);
        var choices = ActiveChoices(preceding, cast).ToList();
        if (choices.Count > 0) return choices;
        var required = node.OperationName() == "then"
            ? AdmissionResolution.Full : AdmissionResolution.None;
        return ResolutionOf(preceding, cast) == required
            ? ActiveChoices(EffectFollowing(node), cast) : [];
    }

    internal static bool SuspendsInsideAnd(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        node.OperationName() == "placeThreat"
        || GuardChildren(node, cast, stateMayChange, bindingMayChange, null).Any(child =>
            SuspendsInsideAnd(
                child.Node, cast, child.StateMayChange, child.BindingMayChange));

}
