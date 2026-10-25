using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

internal static class AbilityExcessDamageLowering
{
    internal static void Validate(CardAbility ability, AbilityEffect root, AbilityLocation location,
        IEnumerable<AbilityEffect> effects)
    {
        if (!effects.Any(effect => effect is AbilityEffect.GrantField { Field: ExcessDamageThreat.Modifier })) return;
        bool direct = root is AbilityEffect.GrantField
        {
            Field: ExcessDamageThreat.Modifier, Until: null, EachCard: false,
            Amount: AbilityNumber.Constant { Value: 1 }, AttackRestriction: null,
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo },
        };
        bool unconditional = ability.Trigger == new AbilityTrigger(null, AbilityType.Constant, "this")
            && ability is { When: null, Cost: null, Limit: null, Maximum: null, AnyPlayer: false };
        if (!direct || !unconditional)
            throw location.Error("excessDamageAsThreat requires a direct unconditional Constant unit grant to attachedTo");
    }
}
