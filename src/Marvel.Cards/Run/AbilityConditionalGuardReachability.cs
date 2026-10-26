using static Marvel.Cards.Run.AbilityPowerSuspension;
using System.Collections.Immutable;
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
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Determines which conditional branches can be reached after preceding effects.
internal static class AbilityConditionalGuardReachability
{
    internal static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange)>
        Of(
            AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
            bool bindingMayChange, AbilityEffect? repeatedEffect)
    {
        var test = ConditionalOf(node, cast).Test;
        bool canSwitch = CanSwitch(test, cast, stateMayChange, bindingMayChange, repeatedEffect);
        var branches = canSwitch
            ? ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(value => value is not null)
            : ActiveConditionalBranch(node, test, cast);
        return branches.Select(value => (value, stateMayChange, bindingMayChange));
    }

    private static bool CanSwitch(AbilityCondition test, AbilityAdmissionScope cast,
        bool stateMayChange, bool bindingMayChange, AbilityEffect? repeatedEffect) =>
        StateCanSwitch(test, cast, stateMayChange, bindingMayChange)
        || cast.Reachability.PaymentMayMutate && PaymentCanChange(test)
        || bindingMayChange && BindingCanChange(test)
        || repeatedEffect is not null && RepeatedEffectCanChange(test, repeatedEffect, cast);

    private static bool StateCanSwitch(AbilityCondition test, AbilityAdmissionScope cast,
        bool stateMayChange, bool bindingMayChange) =>
        test is AbilityCondition.InForm && cast.Reachability.PriorSteps.Count > 0
            ? bindingMayChange && BindingCanChange(test) || PriorStepCanChange(test, cast)
            : stateMayChange;

    private static IEnumerable<AbilityEffect> ActiveConditionalBranch(
        AbilityEffect node, AbilityCondition test, AbilityAdmissionScope cast) =>
        ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
            ? [active]
            : [];

}
