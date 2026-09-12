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
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityDelayedReachability
{
    private static readonly HashSet<string> AlwaysAddressedOperations =
    [
        "afterActivation", "eachPlayer", "attack", "thwart", "thwartSchemes",
    ];

    private static readonly HashSet<string> ContinuationSensitiveOperations =
    [
        "placeThreat", "enemyAttacks", "enemySchemes",
    ];

    internal static void ValidateEachTimeBody(
        AbilityEffect node, AbilityAdmissionContext context) =>
        ValidateEachTimeBody(node, new AbilityAdmissionScope(context, []));

    internal static void ValidateEachTimeBody(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (ContainsUnreconstructibleAfterActivation(
            EffectFollowing(node), cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside an after-activation effect, "
                + "which cannot be reconstructed");
        }
    }

    internal static bool ContainsUnreconstructibleAfterActivation(
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

    internal static bool DelayedNeedsContinuationAddress(
        AbilityEffect node, AbilityAdmissionScope cast, bool hasContinuation)
    {
        string operation = node.OperationName();
        if (AlwaysAddressedOperations.Contains(operation)
            || operation == "and" && OrderedEffects(node).Skip(1).Any()
            || IsChoice(node))
        {
            return true;
        }
        if (ContinuationSensitiveOperations.Contains(operation))
        {
            return hasContinuation;
        }
        return operation switch
        {
            "seq" or "and" => OrderedNeedsAddress(node, cast, hasContinuation),
            "if" => ConditionalNeedsAddress(node, cast, hasContinuation),
            "then" or "otherwise" => DependentNeedsAddress(node, cast, hasContinuation),
            "forEach" => ForEachNeedsAddress(node, cast, hasContinuation),
            "eachTime" => EachTimeNeedsAddress(node, cast, hasContinuation),
            _ => ContinuationChildren(node).Any(child =>
                DelayedNeedsContinuationAddress(child, cast, hasContinuation)),
        };
    }

    private static bool OrderedNeedsAddress(
        AbilityEffect node, AbilityAdmissionScope cast, bool hasContinuation)
    {
        var children = OrderedEffects(node).ToList();
        return children.Select((child, index) => (child, index)).Any(entry =>
            DelayedNeedsContinuationAddress(
                entry.child, cast,
                hasContinuation || entry.index < children.Count - 1));
    }

    private static bool ConditionalNeedsAddress(
        AbilityEffect node, AbilityAdmissionScope cast, bool hasContinuation) =>
        ConditionalBranches((AbilityEffect.Conditional)node)
            .Where(branch => branch is not null)
            .Any(branch => DelayedNeedsContinuationAddress(
                branch, cast, hasContinuation));

    private static bool DependentNeedsAddress(
        AbilityEffect node, AbilityAdmissionScope cast, bool hasContinuation) =>
        DelayedNeedsContinuationAddress(EffectBody(node), cast, hasContinuation: true)
        || DelayedNeedsContinuationAddress(
            EffectFollowing(node), cast, hasContinuation);

    private static bool ForEachNeedsAddress(
        AbilityEffect node, AbilityAdmissionScope cast, bool hasContinuation)
    {
        if (AmountMayChange(ForEachOf(node, cast).Count))
        {
            return DelayedNeedsContinuationAddress(
                EffectBody(node), cast, hasContinuation: true);
        }
        long count = ForEachCount(node, cast);
        return count > 0 && DelayedNeedsContinuationAddress(
            EffectBody(node), cast, hasContinuation || count > 1);
    }

    private static bool EachTimeNeedsAddress(
        AbilityEffect node, AbilityAdmissionScope cast, bool hasContinuation)
    {
        if (EachTimeOf(node, cast).Effect is not AbilityEffect.DiscardTop
            { From: AbilitySearchArea.EncounterDeck, Players: null } preceding)
        {
            return true;
        }
        if (AmountMayChange(preceding.Count)) return true;
        long count = Amount(preceding.Count, cast);
        if (count < 0)
        {
            throw new AbilityException("'eachTime' needs a non-negative discard count");
        }
        return count > 0 && DelayedNeedsContinuationAddress(
            EffectFollowing(node), cast, hasContinuation || count > 1);
    }

}
