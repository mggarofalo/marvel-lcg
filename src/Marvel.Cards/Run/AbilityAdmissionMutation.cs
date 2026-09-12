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
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmissionMutation;
using static Marvel.Cards.Run.AbilityBindingReachability;
using static Marvel.Cards.Run.AbilityInitiationConstraints;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityAdmissionMutation
{
    internal static void PreflightDependentOutcomesAfterMutation(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node.OperationName() == "forEach" && SkipForEachPreflight(node, cast))
        {
            return;
        }

        if (node.OperationName() is "then" or "otherwise")
        {
            PreflightResolutionBranches(
                EffectBody(node), cast, allBranches: true);
        }

        var children = node.OperationName() switch
        {
            "choose" => ((AbilityEffect.Choose)node).Options,
            "eachPlayer" => [EffectBody(node)],
            _ => ResolutionChildren(node),
        };
        foreach (var child in children)
        {
            PreflightDependentOutcomesAfterMutation(child, cast);
        }
    }

    internal static bool CanInitiateIf(AbilityEffect node, AbilityAdmissionScope cast)
    {
        // Payment happens after an action is offered and can change the facts
        // tested by the branch. Validate every structurally reachable
        // continuation boundary now, while no cost has been paid, then use
        // only the currently active branch for ordinary target eligibility.
        var test = ConditionalOf(node, cast).Test;
        bool paymentCanSwitch = cast.Reachability.PaymentMayMutate && PaymentCanChange(test);
        bool bindingCanSwitch = cast.Reachability.PriorBindingMayChange
            && BindingCanChange(test);
        bool stateCanSwitch = bindingCanSwitch
            || PriorStepCanChange(test, cast) || paymentCanSwitch;
        if (!PreflightConditionalBranches(
                (AbilityEffect.Conditional)node, cast, stateCanSwitch, paymentCanSwitch))
        {
            return false;
        }
        if (bindingCanSwitch)
        {
            return ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(value => value is not null)
                .All(value => CanInitiate(value, cast));
        }
        return ConditionalBranch(node, Test(test, cast) ? "then" : "else")
            is not { } active || CanInitiate(active, cast);
    }

    private static bool PreflightConditionalBranches(
        AbilityEffect.Conditional conditional, AbilityAdmissionScope cast,
        bool stateCanSwitch, bool requireCurrentTargets)
    {
        bool valid = true;
        foreach (var effect in ConditionalBranches(conditional).Where(value => value is not null))
        {
            PreflightContinuationBoundaries(effect, cast);
            if (!stateCanSwitch) continue;
            PreflightDependentOutcomesAfterMutation(effect, cast);
            PreflightInitiationConstraints(
                effect, cast, requireCurrentTargets: requireCurrentTargets);
            if (HasLabelledPower(effect) && !CanInitiate(effect, cast)) valid = false;
        }
        return valid;
    }

    internal static bool PriorStepCanChange(AbilityCondition test, AbilityAdmissionScope cast) => test switch
    {
        AbilityCondition.All all => all.Operands.Any(child =>
            PriorStepCanChange(child, cast)),
        AbilityCondition.Any any => any.Operands.Any(child =>
            PriorStepCanChange(child, cast)),
        AbilityCondition.Negated negated => PriorStepCanChange(negated.Operand, cast),
        AbilityCondition.InForm form => cast.Reachability.PriorBindingMayChange
                && BindingCanChange(test)
            || SeatMayChange(cast.Reachability.PriorFormsMayChange, Seat(form.Player, cast)),
        _ => cast.Reachability.PriorStepMayMutate,
    };

    /// <summary>Seats whose final form may differ after a reachable effect.</summary>
    internal static ulong FormsMayDifferAfter(
        AbilityEffect node, AbilityAdmissionScope cast, ulong before,
        bool bindingMayChange = false)
    {
        if (node.OperationName() == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.OperationName() == "changeForm")
        {
            return FormsAfterChange(node, cast, before, bindingMayChange);
        }

        cast = cast.ForReachability(cast.Reachability with { PriorFormsMayChange = before });
        if (node.OperationName() == "if")
        {
            return FormsAfterConditional(node, cast, before, bindingMayChange);
        }
        if (node.OperationName() == "choose")
        {
            return FormsAfterChoice((AbilityEffect.Choose)node, cast, before, bindingMayChange);
        }
        if (node.OperationName() == "eachPlayer")
        {
            return FormsAfterEachPlayer(node, cast, before, bindingMayChange);
        }

        ulong state = before;
        bool childBindingMayChange = bindingMayChange
            || node.OperationName() is "chooseCard" or "thwartSchemes"
                or "thwartDifferentSchemes" or "legalPractice";
        foreach (var child in MutationChildren(node))
        {
            cast = cast.ForReachability(cast.Reachability with { PriorFormsMayChange = state });
            state = FormsMayDifferAfter(
                child, cast, state, childBindingMayChange);
        }
        return state;
    }

    private static ulong FormsAfterConditional(
        AbilityEffect node, AbilityAdmissionScope cast, ulong before, bool bindingMayChange)
    {
        var test = ConditionalOf(node, cast).Test;
        bool canSwitch = bindingMayChange && BindingCanChange(test)
            || PriorStepCanChange(test, cast)
            || cast.Reachability.PaymentMayMutate && PaymentCanChange(test);
        var branches = canSwitch
            ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
            : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                ? [active] : [];
        return branches.Select(branch =>
                FormsMayDifferAfter(branch, cast, before, bindingMayChange))
            .DefaultIfEmpty(before)
            .Aggregate((left, right) => left | right);
    }

    private static ulong FormsAfterChoice(
        AbilityEffect.Choose choice, AbilityAdmissionScope cast,
        ulong before, bool bindingMayChange) =>
        choice.Options.Select(option =>
                FormsMayDifferAfter(option, cast, before, bindingMayChange))
            .DefaultIfEmpty(before)
            .Aggregate((left, right) => left | right);

    private static ulong FormsAfterChange(
        AbilityEffect node, AbilityAdmissionScope cast, ulong before, bool bindingMayChange)
    {
        var change = FormChangeOf(node, cast);
        bool canChangePlayer = change.Player == AbilityPlayer.ChosenPlayer;
        var seats = bindingMayChange && canChangePlayer
            ? cast.World.PlayerOrder.ToList()
            : [Seat(change.Player, cast)];
        ulong ChangeOne(ulong state, int seat)
        {
            ulong bit = PlayerSeat(seat);
            return Forms.In(
                    cast.World, cast.World.Seats[seat], cast.World.Facts, change.Form)
                ? state & ~bit : state | bit;
        }
        if (bindingMayChange && canChangePlayer)
        {
            return seats.Aggregate(0UL, (possible, seat) => possible | ChangeOne(before, seat));
        }
        return seats.Aggregate(before, ChangeOne);
    }

    private static ulong FormsAfterEachPlayer(
        AbilityEffect node, AbilityAdmissionScope cast, ulong before, bool bindingMayChange)
    {
        int original = cast.Player;
        try
        {
            ulong possible = 0;
            foreach (var order in PlayerPermutations(cast.World.PlayerOrder.ToList()))
            {
                ulong after = before;
                foreach (int player in order)
                {
                    cast.RestorePlayer(player);
                    cast = cast.ForReachability(
                        cast.Reachability with { PriorFormsMayChange = after });
                    after = FormsMayDifferAfter(EffectBody(node), cast, after, bindingMayChange);
                }
                possible |= after;
            }
            return possible;
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    internal static IEnumerable<IReadOnlyList<int>> PlayerPermutations(
        List<int> players)
    {
        if (players.Count == 0)
        {
            yield return [];
            yield break;
        }
        for (int index = 0; index < players.Count; index++)
        {
            int player = players[index];
            var rest = players.Where((_, candidate) => candidate != index).ToList();
            foreach (var tail in PlayerPermutations(rest))
            {
                yield return [player, .. tail];
            }
        }
    }

    internal static bool BindingMayChangeAfter(
        AbilityEffect node, AbilityAdmissionScope cast, bool before)
    {
        if (node.OperationName() is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            return true;
        }
        if (node.OperationName() == "forEach" && StableZeroForEach(node, cast))
        {
            return before;
        }
        if (node.OperationName() == "if")
        {
            return BindingAfterConditional(node, cast, before);
        }
        bool after = before;
        foreach (var child in MutationChildren(node))
        {
            after = BindingMayChangeAfter(child, cast, after);
        }
        return after;
    }

    private static bool BindingAfterConditional(
        AbilityEffect node, AbilityAdmissionScope cast, bool before)
    {
        var test = ConditionalOf(node, cast).Test;
        bool canSwitch = before && BindingCanChange(test)
            || PriorStepCanChange(test, cast)
            || cast.Reachability.PaymentMayMutate && PaymentCanChange(test);
        var branches = canSwitch
            ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
            : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                ? [active] : [];
        return branches.Any(branch => BindingMayChangeAfter(branch, cast, before));
    }

}
