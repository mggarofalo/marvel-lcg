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
using Marvel.Rules.State;

using static Marvel.Cards.Run.AbilityAdmissionResolutionPreflight;
namespace Marvel.Cards.Run;

internal static class AbilityAdmissionResolutionPreflight
{
    private static readonly HashSet<string> BindingChangingGuardOperations =
    [
        "chooseCard", "thwartSchemes", "thwartDifferentSchemes", "legalPractice",
    ];

    private static readonly HashSet<string> StateChangingGuardOperations =
    [
        "afterActivation", "delayUntil", "defense", "payOrEffect", "payOrExhaust",
    ];

    internal static void PreflightResolutionBranches(
        AbilityEffect node, AbilityAdmissionScope cast, bool allBranches = false)
    {
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            var branches = allBranches || cast.Reachability.PriorStepMayMutate || PaymentCanChange(test)
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            foreach (var branch in branches)
            {
                PreflightResolutionBranches(branch, cast, allBranches);
            }
            return;
        }

        _ = ResolutionOf(node, cast);
    }

    internal static bool PaymentCanChange(AbilityCondition test) => test switch
    {
        AbilityCondition.All all => all.Operands.Any(PaymentCanChange),
        AbilityCondition.Any any => any.Operands.Any(PaymentCanChange),
        AbilityCondition.Negated negated => PaymentCanChange(negated.Operand),

        // Paying an ability cannot change identity form. Other predicates may
        // read the chosen resources, the source's in-play status, or another
        // fact changed by an authored cost, so their branches are preflighted
        // conservatively.
        AbilityCondition.InForm => false,
        _ => true,
    };

    internal static bool ContainsNode(AbilityEffect node, string kind, AbilityAdmissionScope cast) =>
        node.OperationName() == kind
        || !StableZeroForEach(node, cast)
            && ResolutionChildren(node).Any(child => ContainsNode(child, kind, cast));

    internal static bool HasNestedEachPlayer(
        AbilityEffect node, AbilityAdmissionScope cast, bool inside = false, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityEffect? repeatedEffect = null)
    {
        if (inside && node.OperationName() == "eachPlayer")
        {
            return true;
        }
        if (node.OperationName() == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (HasNestedEachPlayer(
                        EffectBody(node), cast, inside: true,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? EffectBody(node) : repeatedEffect))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        bool within = inside || node.OperationName() == "eachPlayer";
        return GuardChildren(
            node, cast, stateMayChange, bindingMayChange, repeatedEffect).Any(child =>
            HasNestedEachPlayer(
                child.Node, cast, within, child.StateMayChange,
                child.BindingMayChange, repeatedEffect));
    }

    internal static bool ContainsUnsupportedPower(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityEffect? repeatedEffect = null)
    {
        if (node.OperationName() == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (ContainsUnsupportedPower(
                        EffectBody(node), cast,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? EffectBody(node) : repeatedEffect))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        if (node.OperationName() is "attack" or "thwart")
        {
            var prior = cast.CaptureChosen();
            try
            {
                var target = Find(EffectOf<AbilityEffect.Power>(node, cast).Target!, cast);
                bool targetWillBind = target is null;
                if (target is not null)
                {
                    cast.Choose(target);
                }
                if (SuspendsPowerEffect(
                    EffectBody(node), cast, stateMayChange,
                    bindingMayChange || targetWillBind))
                {
                    return true;
                }
            }
            finally
            {
                cast.RestoreChosen(prior);
            }
        }
        if (node.OperationName() == "thwartSchemes")
        {
            var power = ((AbilityEffect.ThwartGroup)node).Thwart;
            if (SuspendsPowerEffect(
                EffectBody(power), cast, stateMayChange, bindingMayChange))
            {
                return true;
            }
        }
        return GuardChildren(
            node, cast, stateMayChange, bindingMayChange, repeatedEffect).Any(child =>
            ContainsUnsupportedPower(
                child.Node, cast, child.StateMayChange,
                child.BindingMayChange, repeatedEffect));
    }

    /// <summary>Executable children that can be reached after an ability is offered.</summary>
    internal static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange)> GuardChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange, bool bindingMayChange,
        AbilityEffect? repeatedEffect)
    {
        string operation = node.OperationName();
        if (operation == "forEach" && ForEachHasNoGuardChildren(
            node, cast, stateMayChange, bindingMayChange))
        {
            return [];
        }
        if (operation is "seq" or "and")
        {
            return OrderedGuardChildren(node, stateMayChange, bindingMayChange);
        }
        if (operation == "if")
        {
            return ConditionalGuardChildren(
                node, cast, stateMayChange, bindingMayChange, repeatedEffect);
        }
        if (operation is "then" or "otherwise")
        {
            return DependentGuardChildren(node, cast, stateMayChange, bindingMayChange);
        }
        if (BindingChangingGuardOperations.Contains(operation))
        {
            return ContinuationChildren(node).Select(child =>
                (child, stateMayChange, true));
        }
        if (StateChangingGuardOperations.Contains(operation))
        {
            return ContinuationChildren(node).Select(child =>
                (child, true, bindingMayChange));
        }
        return ContinuationChildren(node).Select(child =>
            (child, stateMayChange, bindingMayChange));
    }

    private static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange)>
        OrderedGuardChildren(
            AbilityEffect node, bool stateMayChange, bool bindingMayChange)
    {
        var children = OrderedEffects(node).ToList();
        if (node.OperationName() == "seq")
        {
            return children.Select((child, index) =>
                (child, stateMayChange || index > 0, bindingMayChange));
        }
        return children.Select(child =>
            (child, stateMayChange || children.Count > 1, bindingMayChange));
    }

    private static bool ForEachHasNoGuardChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange)
    {
        bool countWillBind = bindingMayChange && HasUnboundPowerAmount(node, cast);
        long? count = countWillBind ? null : ForEachCount(node, cast);
        if (!countWillBind
            && (stateMayChange || bindingMayChange || cast.Reachability.PaymentMayMutate)
            && AmountMayChange(ForEachOf(node, cast).Count))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
        }
        return count == 0;
    }

    private static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange)>
        ConditionalGuardChildren(
            AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
            bool bindingMayChange, AbilityEffect? repeatedEffect)
    {
        var test = ConditionalOf(node, cast).Test;
        bool canSwitch = stateMayChange
            || cast.Reachability.PaymentMayMutate && PaymentCanChange(test)
            || bindingMayChange && BindingCanChange(test)
            || repeatedEffect is not null
                && RepeatedEffectCanChange(test, repeatedEffect, cast);
        var branches = canSwitch
            ? ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(value => value is not null)
            : ActiveConditionalBranch(node, test, cast);
        return branches.Select(value => (value, stateMayChange, bindingMayChange));
    }

    private static IEnumerable<AbilityEffect> ActiveConditionalBranch(
        AbilityEffect node, AbilityCondition test, AbilityAdmissionScope cast) =>
        ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
            ? [active]
            : [];

    private static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange)>
        DependentGuardChildren(
            AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
            bool bindingMayChange)
    {
        var effect = EffectBody(node);
        var required = node.OperationName() == "then"
            ? AdmissionResolution.Full
            : AdmissionResolution.None;
        bool answered = ActiveChoices(effect, cast).Any();
        bool dependentCanRun = stateMayChange
            || cast.Reachability.PaymentMayMutate
            || bindingMayChange
            || answered
            || ResolutionOf(effect, cast) == required;
        if (!dependentCanRun) return [(effect, stateMayChange, bindingMayChange)];
        bool predecessorMayMutate = stateMayChange
            || cast.Reachability.PaymentMayMutate
            || node.OperationName() == "then"
            || answered;
        return
        [
            (effect, stateMayChange, bindingMayChange),
            (EffectFollowing(node), predecessorMayMutate, bindingMayChange),
        ];
    }

}
