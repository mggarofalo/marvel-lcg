using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static partial class AbilityInitiation
{
    internal static void ValidateEachTimeBody(
        AbilityEffect node, AbilityAdmissionContext context) =>
        ValidateEachTimeBody(node, new AbilityAdmissionScope(context, []));

    private static void ValidateEachTimeBody(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (ContainsUnreconstructibleAfterActivation(
            EffectFollowing(node), cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside an after-activation effect, "
                + "which cannot be reconstructed");
        }
    }

    private static bool ContainsUnreconstructibleAfterActivation(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node.OperationName() == "afterActivation")
        {
            return DelayedNeedsContinuationAddress(
                EffectBody(node), cast, hasContinuation: false);
        }
        return ContinuationChildren(node).Any(child =>
            ContainsUnreconstructibleAfterActivation(child, cast));
    }

    private static bool DelayedNeedsContinuationAddress(
        AbilityEffect node, AbilityAdmissionScope cast, bool hasContinuation)
    {
        if (node.OperationName() == "afterActivation"
            || node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any()
            || IsChoice(node)
            || node.OperationName() is "eachPlayer" or "attack" or "thwart" or "thwartSchemes")
        {
            return true;
        }
        if (node.OperationName() is "placeThreat" or "enemyAttacks" or "enemySchemes")
        {
            return hasContinuation;
        }
        if (node.OperationName() is "seq" or "and")
        {
            var children = OrderedEffects(node).ToList();
            return children.Select((child, index) => (child, index)).Any(entry =>
                DelayedNeedsContinuationAddress(
                    entry.child, cast,
                    hasContinuation || entry.index < children.Count - 1));
        }
        if (node.OperationName() == "if")
        {
            return ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(branch => branch is not null)
                .Any(branch => DelayedNeedsContinuationAddress(
                    branch, cast, hasContinuation));
        }
        if (node.OperationName() is "then" or "otherwise")
        {
            return DelayedNeedsContinuationAddress(
                    EffectBody(node), cast, hasContinuation: true)
                || DelayedNeedsContinuationAddress(
                    EffectFollowing(node), cast, hasContinuation);
        }
        if (node.OperationName() == "forEach")
        {
            if (AmountMayChange(ForEachOf(node, cast).Count))
            {
                return DelayedNeedsContinuationAddress(
                    EffectBody(node), cast, hasContinuation: true);
            }
            long count = ForEachCount(node, cast);
            return count > 0 && DelayedNeedsContinuationAddress(
                EffectBody(node), cast,
                hasContinuation || count > 1);
        }
        if (node.OperationName() == "eachTime")
        {
            if (EachTimeOf(node, cast).Effect is not AbilityEffect.DiscardTop
                { From: AbilitySearchArea.EncounterDeck, Players: null } preceding)
            {
                return true;
            }

            var requested = preceding.Count;
            if (AmountMayChange(requested))
            {
                return true;
            }
            long count = Amount(requested, cast);
            if (count < 0)
            {
                throw new AbilityException("'eachTime' needs a non-negative discard count");
            }
            if (count == 0)
            {
                return false;
            }
            return DelayedNeedsContinuationAddress(
                EffectFollowing(node), cast,
                hasContinuation || count > 1);
        }
        return ContinuationChildren(node).Any(child =>
            DelayedNeedsContinuationAddress(child, cast, hasContinuation));
    }

}
