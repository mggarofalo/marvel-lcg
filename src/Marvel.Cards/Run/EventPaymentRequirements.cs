using static Marvel.Cards.Run.AbilityPaymentRules;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record EventPaymentRequirements(
    long Total, string Required, List<ResourceCost> Components)
{
    internal static EventPaymentRequirements Create(
        World world, Card card, AdjustedCardCost adjusted, AbilityCost? additionalCost, long? energy, CompiledCardAbility ability)
    {
        string printed = AbilityPaymentPricing.PrintedRequirement(world, card, ability, adjusted.Amount);
        string additional = energy is { } x
            ? new string(Resources.Energy, checked((int)x)) : ResourceRequirement(additionalCost, card);
        int additionalAmount = checked(additional.Length + GenericResourceAmount(additionalCost));
        var components = new List<ResourceCost>
        {
            new(adjusted.Amount.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
                printed.Length > 0 ? [printed] : null),
        };
        if (additionalAmount > 0)
            components.Add(new ResourceCost(
                additionalAmount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture), [additional]));
        // Total is an early affordability check. Component allocation independently
        // enforces the full amount, so omitting this addition is an equivalent
        // mutant when every component is checked by ResourceAllocationLedger.
        return new EventPaymentRequirements(
            checked(adjusted.Amount + additionalAmount), printed + additional, components);
    }
}
