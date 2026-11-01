using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>Admits effects that read or change excess damage only where that excess exists.</summary>
internal static class AbilityExcessDamageLowering
{
    internal static void Validate(CardAbility ability, AbilityEffect root, AbilityLocation location,
        IEnumerable<AbilityEffect> effects)
    {
        ValidateIncrease(ability, location, effects);
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

    // The imminent excess exists only in an attack's damage step 1, where the
    // excess-damage interrupt is offered.
    private static void ValidateIncrease(CardAbility ability, AbilityLocation location, IEnumerable<AbilityEffect> effects)
    {
        if (effects.Any(effect => effect is AbilityEnemyDamageEffect.IncreaseExcessDamage)
            && (ability.Trigger.Event != ExcessDamageTiming.AttackExcessDamageWouldBeDealt
                || !AbilityTypes.IsInterrupt(ability.Trigger.Timing)))
            throw location.Error("increaseExcessDamage requires the attack excess-damage interrupt");
    }
}
