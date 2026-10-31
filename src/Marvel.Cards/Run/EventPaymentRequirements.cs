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
        // rr:non-numerical-variable.1: a reduction lowers the amount paid for X, not X.
        long? paidForX = energy is { } x ? Math.Max(0, x - adjusted.VariableReduction) : null;
        string additional = paidForX is { } paying && additionalCost is AbilityCost.SpendX variable
            ? variable.Required(paying) : ResourceRequirement(additionalCost, card);
        int additionalAmount = paidForX is { } defined
            ? checked((int)defined) : checked(additional.Length + GenericResourceAmount(additionalCost));
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
