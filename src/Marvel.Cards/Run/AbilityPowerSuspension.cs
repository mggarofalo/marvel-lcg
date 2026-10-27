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
        bool bindingMayChange = false, PowerReachability? reachability = null, bool terminal = true)
    {
        // The enemy activation resolves through the ordinary rules procedure. Its
        // continuation retains the power and exact chosen incarnation; fixed damage
        // reads the settled live board without projecting the enemy's attack.
        if (HasRetainedBody(node)) return false;
        // The observation's receipt can feed the bounded chosen-recipient tail.
        // Arbitrary later deck reads would need an additional state projection.
        if (AbilityPowerSimultaneousSupport.ContainsPair(node)
            && !AbilityPowerSimultaneousSupport.SupportsBody(node)) return true;
        var state = reachability ?? AbilityPowerStateFactory.Initial(cast);
        // Enumerate every supported path before accepting a suspension answer;
        // unsupported is a missing calculation, never an absent legal option.
        var paths = PowerPaths(state).ToArray();
        if (AbilityPowerContinuationDependencies.IsIndependent(node)) return false;
        if (AbilityStageEntryDependencies.CanFinishLive(node, cast, terminal, paths)) return false;
        foreach (var path in paths) AbilityProjectedDefeatWindows.ValidateContinuation(node, path, cast);
        // This closed tail reads only the retained discard receipt and chosen
        // reference. Its fixed damage/status instructions resolve on the live
        // board after stage reveal; no later admission decision consumes a
        // predicted stage result. The damage procedure owns reveal suspension.
        // rr:and: independent effects resolve simultaneously. The structural
        // order prompt retains the active power; only these bounded pairs have
        // an observation that cannot alter the mutation's recipient or amount.
        var mutation = AbilityPowerSimultaneousSupport.SuspensionMutation(node);
        if (mutation is not null)
            return paths.Any(path => SuspendsPowerEffect(mutation, cast,
                stateMayChange, bindingMayChange, path, terminal: false));
        return HasIntrinsicSuspension(node)
            || paths.SelectMany(path => PowerSuspensionChildren(
                node, cast, stateMayChange, bindingMayChange, path)).Any(child =>
                SuspendsPowerEffect(
                    child.Node, cast, child.StateMayChange, child.BindingMayChange,
                    child.Reachability, IsTerminalChild(node, child.Node, terminal)));
    }

    private static bool HasRetainedBody(AbilityEffect node) =>
        AbilityStagedAttackLowering.IsBody(node) || node is AbilityEffect.AttackAnotherEnemy
            || AbilityPowerActivationSupport.SupportsBody(node);

    private static bool HasIntrinsicSuspension(AbilityEffect node) =>
        (node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any())
            || IsChoice(node)
            || node.OperationName() is "eachPlayer" or "attack" or "thwart" or "thwartSchemes"
                or "placeThreat" or "enemyAttacks" or "enemySchemes"
                or "revealFromVillainDeck" or "reveal" or "revealTop" or "revealAuxiliaryTop"
                or "search" or "discardUntil";

    private static bool IsTerminalChild(AbilityEffect parent, AbilityEffect child, bool terminal) => terminal && parent switch
    {
        AbilityEffect.Sequence sequence => ReferenceEquals(sequence.Effects[^1], child),
        AbilityEffect.Conditional => true,
        _ => false,
    };

    internal static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerSuspensionChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability) =>
        node.OperationName() == "seq"
            ? PowerSequenceChildren(node, cast, stateMayChange, bindingMayChange,
                reachability, suspensionOnly: true)
            : PowerProjectionChildren(node, cast, stateMayChange, bindingMayChange, reachability);

    internal static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerProjectionChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (node.OperationName() == "seq")
        {
            return PowerSequenceChildren(
                node, cast, stateMayChange, bindingMayChange, reachability);
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
        if (node.OperationName() is "then" or "otherwise")
            return AbilityPowerDependentChildren.Of(
                node, cast, stateMayChange, bindingMayChange, reachability);
        return GuardChildren(node, cast, stateMayChange, bindingMayChange, null)
            .Select(child =>
                (child.Node, child.StateMayChange, child.BindingMayChange,
                    reachability));
    }

    private static List<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerSequenceChildren(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability, bool suspensionOnly = false)
    {
        var children = OrderedEffects(node).ToList();
        var result = new List<(AbilityEffect, bool, bool, PowerReachability)>(children.Count);
        var state = reachability;
        for (int index = 0; index < children.Count; index++)
        {
            bool mayChange = stateMayChange || index > 0;
            result.Add((children[index], mayChange, bindingMayChange, state));
            if (index + 1 < children.Count)
            {
                // Admission still projects the prefix to check damage batches and
                // unsupported departures. An independent final tail needs no
                // predicted result from a live minion defeat interrupt.
                bool liveSettlement = suspensionOnly && children.Skip(index + 1)
                    .All(AbilityPowerContinuationDependencies.IsIndependent);
                state = MergePowerAlternatives(PowerPaths(state).Select(path =>
                    PowerStateAfter(children[index], cast, mayChange, bindingMayChange,
                        path with { LiveMinionDefeatSettlement = liveSettlement })));
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
