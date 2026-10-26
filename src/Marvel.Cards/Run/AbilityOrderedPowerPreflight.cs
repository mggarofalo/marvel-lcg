using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

// Carry the preceding sequence's form and departure facts into guard analysis.
internal static class AbilityOrderedPowerPreflight
{
    internal static bool ContainsUnsupported(
        AbilityEffect sequence, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityEffect? repeatedEffect)
    {
        var before = cast.Reachability;
        var prior = before.PriorSteps;
        ulong forms = before.PriorFormsMayChange;
        int index = 0;
        foreach (var child in AbilityEffectStructure.OrderedEffects(sequence))
        {
            var scope = cast.ForReachability(before with
            {
                PriorSteps = prior,
                PriorFormsMayChange = forms,
                PriorBindingMayChange = bindingMayChange,
                PriorStepMayMutate = before.PriorStepMayMutate || index > 0,
            });
            if (AbilityAdmissionResolutionPreflight.ContainsUnsupportedPower(
                child, scope, stateMayChange || index > 0, bindingMayChange, repeatedEffect))
                return true;
            forms = AbilityAdmissionMutation.FormsMayDifferAfter(child, scope, forms, bindingMayChange);
            bindingMayChange = AbilityAdmissionMutation.BindingMayChangeAfter(child, scope, bindingMayChange);
            prior = prior.Add(child);
            index++;
        }
        return false;
    }
}
