using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityPaymentRules;

namespace Marvel.Cards.Run;

/// <summary>Assesses all components of one simultaneous arrow cost together.</summary>
internal static class AbilitySequenceCostAvailability
{
    internal static bool CanPay(
        World world, Card card, int player, AbilityCost.Sequence cost,
        AbilityProgram program, IResourceCardAbilities resourceAbilities)
    {
        // rr:cost.5: multiple costs "must be paid simultaneously". Nesting is
        // a DSL grouping choice, so every component shares the complete cost.
        cost = new AbilityCost.Sequence([.. cost.Costs.SelectMany(Flatten)]);
        var steps = cost.Costs;
        if (!PhysicalCostsPayable(world, card, player, cost))
        {
            return false;
        }

        var spends = steps.OfType<AbilityCost.Spend>().ToList();
        if (spends.Count > 0)
        {
            if (player < 0)
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has simultaneous resource costs and is offered to "
                    + "every player, so there is no hand to price them against");
            }

            bool printed = spends.All(step => step.PrintedOnly);
            if (!printed && spends.Any(step => step.PrintedOnly))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' mixes printed and ordinary simultaneous "
                    + "resource costs, whose allocation is not implemented");
            }

            var price = AbilityPaymentPricing.Price(world, card, player, cost, resourceAbilities)!;
            if (ResourcePayment.Allocate(price, price.Generators.Select(source => source.Effect).ToArray()) is null)
                return false;
        }

        return steps.Where(step => step is not (AbilityCost.Spend or AbilityCost.RemoveCounters))
            .All(step => Payable(
                world, card, player, step, program, resourceAbilities, cost));
    }

    private static bool PhysicalCostsPayable(World world, Card card, int player, AbilityCost.Sequence cost) =>
        AbilityRandomHandCost.CanPayTogether(world, card, player, cost.Costs)
        && CounterCostsPayable(world, card, player, cost)
        && AbilityDeckCosts.CanPayTogether(world, player, cost.Costs);

    private static IEnumerable<AbilityCost> Flatten(AbilityCost cost) =>
        cost is AbilityCost.Sequence sequence ? sequence.Costs.SelectMany(Flatten) : [cost];
}
