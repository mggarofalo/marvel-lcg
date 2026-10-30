using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

// Only a standalone, unconditional self exclusion has settlement support.
internal static class AbilityAllyLimitExclusionLowering
{
    internal static void Validate(AbilityProgram program)
    {
        foreach (var ability in program.Abilities)
        {
            var nodes = program.Effects.Where(pair => pair.Key.Card == ability.Card
                && pair.Key.Ability == ability.Address.Ability).Select(pair => pair.Value);
            if (!nodes.Any(node => node is AbilityEffect.GrantField { Field: AllyLimitExclusion.Modifier })) continue;
            if (ability.Trigger.Timing != AbilityType.Constant
                || ability.Effect is not AbilityEffect.GrantField
                { Field: AllyLimitExclusion.Modifier, Cards: AbilityCardSelection.Bound
                    { Binding: AbilityCardBinding.This }, EachCard: false, Until: null,
                    Amount: AbilityNumber.Constant { Value: 1 } })
                throw new AbilityException("ally-limit exclusion requires a standalone constant self grant");
        }
    }
}
