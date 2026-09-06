using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static partial class AbilityInitiation
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

        var children = node.OperationName() switch
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

        foreach (var found in children.SelectMany(Choices))
        {
            yield return found;
        }
    }

    internal static bool IsChoice(AbilityEffect node) =>
        node.OperationName() is "choose" or "chooseCard" or "indirectDamage"
            or "resolveSpecials" or "payOrExhaust" or "chooseTopForHand"
            or "chooseDiscardToShuffle" or "thwartDifferentSchemes" or "makeTheCall"
            or "legalPractice" or "payOrEffect" or "enemyAttacks" or "enemySchemes";

    /// <summary>Choice nodes on the control-flow path that can execute now.</summary>
    private static IEnumerable<AbilityEffect> ActiveChoices(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (CurrentlyZeroForEach(node, cast))
        {
            yield break;
        }

        if (node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any())
        {
            yield return node;
            yield break;
        }

        if (node.OperationName() is "enemyAttacks" or "enemySchemes")
        {
            if (ActivationCandidates(ActivationOf(node, cast), cast).Count > 1)
            {
                yield return node;
            }
            yield break;
        }

        if (IsChoice(node))
        {
            if (node.OperationName() != "indirectDamage"
                || Assignable(((AbilityEffect.IndirectDamage)
                    node).Among, cast).Count > 1)
            {
                yield return node;
            }
            yield break;
        }

        if (node.OperationName() is "then" or "otherwise")
        {
            var preceding = EffectBody(node);
            var precedingChoices = ActiveChoices(preceding, cast).ToList();
            foreach (var found in precedingChoices)
            {
                yield return found;
            }
            if (precedingChoices.Count > 0)
            {
                yield break;
            }

            var required = node.OperationName() == "then"
                ? ResolutionOutcome.Full
                : ResolutionOutcome.None;
            if (ResolutionOf(preceding, cast) == required)
            {
                foreach (var found in ActiveChoices(
                    EffectFollowing(node), cast))
                {
                    yield return found;
                }
            }
            yield break;
        }

        var children = node.OperationName() switch
        {
            "seq" or "and" => OrderedEffects(node),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch ? [branch] : [],
            "eachPlayer" or "forEach" => [EffectBody(node)],
            "defense" => [EffectBody(node)],
            _ => [],
        };

        foreach (var found in children.SelectMany(child => ActiveChoices(child, cast)))
        {
            yield return found;
        }
    }

    private static bool SuspendsInsideAnd(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        node.OperationName() == "placeThreat"
        || GuardChildren(node, cast, stateMayChange, bindingMayChange, null).Any(child =>
            SuspendsInsideAnd(
                child.Node, cast, child.StateMayChange, child.BindingMayChange));

}
