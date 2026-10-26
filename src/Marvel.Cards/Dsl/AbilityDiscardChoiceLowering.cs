using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Bounds the pre-arrow discard choice to its supported exhaustion and recovery.</summary>
internal static class AbilityDiscardChoiceLowering
{
    internal static void Validate(CardAbility ability, AbilityCost? cost, AbilityEffect effect,
        AbilityLocation location)
    {
        if (!Contains(cost)) return;
        // Engine choice: admit only this complete current requirement. Other cost
        // compositions need their own atomicity and continuation proof.
        if (cost is not AbilityCost.Sequence { Costs.Length: 2 } sequence
            || sequence.Costs.Count(step => step is AbilityCost.Exhaust { Card: AbilityCostCard.Source }) != 1
            || sequence.Costs.Count(step => step is AbilityCost.ChooseFromDiscard) != 1
            || ability.Trigger.Timing != AbilityType.Action
            || ability.Labels is { Count: > 0 }
            || effect is not AbilityEffect.CardAction
                { Instruction: AbilityCardInstruction.AddToHand,
                  Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen } })
            throw location.Error("chooseFromDiscard requires source exhaustion and addToHand chosen in an Action");
    }

    private static bool Contains(AbilityCost? cost) => cost switch
    {
        AbilityCost.ChooseFromDiscard => true,
        AbilityCost.Sequence sequence => sequence.Costs.Any(Contains),
        _ => false,
    };
}
