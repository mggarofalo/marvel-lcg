using static Marvel.Cards.Run.AbilityPowerAlternatives;
using static Marvel.Cards.Run.AbilityPowerSuspension;
using static Marvel.Cards.Run.AbilitySelectorTrace;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerStateMutation;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
namespace Marvel.Cards.Run;

internal static class AbilityPowerSuspension
{
    internal static bool SuspendsPowerEffect(
        AbilityEffect effect, AbilityAdmissionContext context,
        bool stateMayChange = false, bool bindingMayChange = false) =>
        SuspendsPowerEffect(
            effect, new AbilityAdmissionScope(context, []),
            stateMayChange, bindingMayChange);

    internal static bool SuspendsPowerEffect(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange = false,
        bool bindingMayChange = false, PowerReachability? reachability = null)
    {
        var state = reachability ?? InitialPowerReachability(cast);
        // Enumerate every supported path before accepting a suspension answer;
        // unsupported is a missing calculation, never an absent legal option.
        var paths = PowerPaths(state).ToArray();
        if (AbilityPowerContinuationDependencies.IsIndependent(node)) return false;
        foreach (var path in paths) AbilityProjectedDefeatWindows.ValidateContinuation(node, path, cast);
        return (node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any())
            || IsChoice(node)
            || node.OperationName() is "eachPlayer" or "attack" or "thwart" or "thwartSchemes"
                or "placeThreat" or "enemyAttacks" or "enemySchemes"
            || paths.SelectMany(path => PowerSuspensionChildren(
                node, cast, stateMayChange, bindingMayChange, path, suspensionOnly: true)).Any(child =>
                SuspendsPowerEffect(
                    child.Node, cast, child.StateMayChange, child.BindingMayChange,
                    child.Reachability));
    }

    internal static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerSuspensionChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability, bool suspensionOnly = false)
    {
        if (node.OperationName() == "seq")
        {
            return PowerSequenceChildren(
                node, cast, stateMayChange, bindingMayChange, reachability, suspensionOnly);
        }
        if (node.OperationName() == "and")
        {
            var children = OrderedEffects(node).ToList();
            return children.Select(child =>
                (child,
                    stateMayChange || children.Count > 1,
                    bindingMayChange,
                    reachability));
        }
        if (node.OperationName() == "if")
        {
            return PowerConditionalChildren(
                node, cast, stateMayChange, bindingMayChange, reachability);
        }
        return GuardChildren(node, cast, stateMayChange, bindingMayChange, null)
            .Select(child =>
                (child.Node, child.StateMayChange, child.BindingMayChange,
                    reachability));
    }

    private static List<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerSequenceChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability, bool suspensionOnly)
    {
        var children = OrderedEffects(node).ToList();
        var result = new List<(AbilityEffect, bool, bool, PowerReachability)>(children.Count);
        var state = reachability;
        for (int index = 0; index < children.Count; index++)
        {
            bool mayChange = stateMayChange || index > 0;
            result.Add((children[index], mayChange, bindingMayChange, state));
            if (index + 1 < children.Count
                && (!suspensionOnly
                    || !children.Skip(index + 1).All(AbilityPowerContinuationDependencies.IsIndependent)))
            {
                state = PowerStateAfter(children[index], cast, mayChange, bindingMayChange, state);
            }
        }
        return result;
    }

    private static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerConditionalChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        var test = ConditionalOf(node, cast).Test;
        bool canSwitch = PowerPaths(reachability).Any(path =>
            PowerTestCanChange(test, cast, stateMayChange, bindingMayChange, path));
        var branches = canSwitch
            ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
            : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                ? [active] : [];
        return branches.Select(value =>
            (value, stateMayChange, bindingMayChange, reachability));
    }

    internal static bool PowerTestCanChange(
        AbilityCondition test, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState reachability) => test switch
        {
            AbilityCondition.All all => all.Operands.Any(child =>
                PowerTestCanChange(
                    child, cast, stateMayChange, bindingMayChange, reachability)),
            AbilityCondition.Any any => any.Operands.Any(child =>
                PowerTestCanChange(
                    child, cast, stateMayChange, bindingMayChange, reachability)),
            AbilityCondition.Negated negated => PowerTestCanChange(
                negated.Operand, cast, stateMayChange,
                bindingMayChange, reachability),
            AbilityCondition.InForm form => bindingMayChange && BindingCanChange(test)
                || FirstPlayerMayRebind(PowerForms(reachability))
                    && form.Player == AbilityPlayer.FirstPlayer
                || SeatMayChange(
                    PowerForms(reachability), Seat(form.Player, cast)),
            _ => stateMayChange
                || cast.Reachability.PaymentMayMutate && PaymentCanChange(test)
                || bindingMayChange && BindingCanChange(test),
        };

}
