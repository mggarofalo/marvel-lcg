using static Marvel.Cards.Run.AbilityPaymentRules;
using static Marvel.Cards.Run.AbilityCostSelection;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityPaymentPricing
{
    /// <summary>What an action's cost looks like on a prompt, or null.</summary>
    /// <remarks>
    /// Resource generation travels in CostOption. Card-valued payments use a
    /// separate TargetRequest from AbilityCostSelection. A cost that names its
    /// own card needs neither choice representation.
    /// </remarks>
    internal static CostOption? Price(
        World world, Card card, int player, AbilityCost? cost,
        IResourceCardAbilities resourceAbilities)
    {
        if (cost is AbilityCost.Sequence sequence)
            return SequencePrice(world, card, player, sequence, resourceAbilities);

        if (cost is AbilityCost.SpendEnergy)
        {
            long maximum = CardPayment.Generators(
                    world, world.Facts, world.Seats[player], resourceAbilities)
                .Sum(source => source.Generates.LongCount(resource =>
                    resource is Resources.Energy or Resources.Wild));
            return new CostOption(
                card.ObjectId, "X",
                Sources: CardPayment.Generators(
                    world, world.Facts, world.Seats[player], resourceAbilities),
                Variables: [new VariableRequest("X", 1, maximum)],
                Components: [new ResourceCost("X") { RepeatedResource = Resources.Energy }]);
        }

        if (cost is AbilityCost.Spend { PrintedOnly: true } printed)
        {
            string printedLetters = printed.Resources;
            return new CostOption(
                Target: card.ObjectId,
                Cost: printedLetters.Length.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Rule: [printedLetters],
                Sources: PrintedGenerators(world, player, resourceAbilities),
                Components: [new ResourceCost(
                    printedLetters.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    [printedLetters],
                    Printed: true)]);
        }

        if (cost is not AbilityCost.Spend spend)
        {
            return null;
        }

        string letters = spend.Resources;
        return new CostOption(
            Target: card.ObjectId,
            Cost: letters.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: [letters],
            Sources: CardPayment.Generators(
                world, world.Facts, world.Seats[player], resourceAbilities));
    }

    private static CostOption? SequencePrice(
        World world, Card card, int player, AbilityCost.Sequence sequence,
        IResourceCardAbilities resourceAbilities)
    {
        var prices = sequence.Costs
            .Select(step => Price(world, card, player, step, resourceAbilities))
            .Where(price => price is not null).Cast<CostOption>().ToList();
        if (prices.Count == 0) return null;
        if (prices.Count == 1) return prices[0];
        if (prices.Any(price => price.HasAlternative)
            || prices.Any(price => !long.TryParse(
                price.Cost, System.Globalization.CultureInfo.InvariantCulture, out _)))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has multiple resource costs whose combined price cannot be represented");
        }
        var components = prices.SelectMany(price => price.ResourceCosts).ToList();
        bool hasPrinted = components.Any(component => component.Printed);
        if (hasPrinted && components.Any(component => !component.Printed))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' mixes printed and ordinary simultaneous resource costs, whose allocation is not implemented");
        }
        long total = prices.Sum(price => long.Parse(
            price.Cost, System.Globalization.CultureInfo.InvariantCulture));
        return new CostOption(
            card.ObjectId, total.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: [string.Concat(prices.SelectMany(price => price.Rule ?? []))],
            Sources: prices.SelectMany(price => price.Generators)
                .GroupBy(source => source.Effect).Select(group => group.First()).ToList(),
            Components: components);
    }

    internal static CostOption? CombinedPrice(
        World world, Card card, int player, CompiledCardAbility ability,
        IResourceCardAbilities resourceAbilities)
    {
        var printed = EventPrice(world, card, player, ability.Effect, resourceAbilities);
        var arrow = Price(world, card, player, ability.Cost, resourceAbilities);
        if (printed is null || arrow is null)
        {
            return printed ?? arrow;
        }

        if (!TryCombinedAmounts(printed, arrow, out long printedAmount, out long arrowAmount))
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' has combined resource costs whose price "
                + "cannot be represented");
        }

        return new CostOption(
            Target: card.ObjectId,
            Cost: checked(printedAmount + arrowAmount).ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            Rule:
            [
                string.Concat(printed.Rule ?? []),
                string.Concat(arrow.Rule ?? []),
            ],
            Sources: printed.Generators
                .Concat(arrow.Generators)
                .GroupBy(source => source.Effect)
                .Select(group => group.First())
                .ToList(),
            Components:
            [
                new ResourceCost(printed.Cost, printed.Rule),
                new ResourceCost(arrow.Cost, arrow.Rule),
            ]);
    }

    private static bool TryCombinedAmounts(
        CostOption printed, CostOption arrow, out long printedAmount, out long arrowAmount)
    {
        bool printedParsed = long.TryParse(
            printed.Cost, System.Globalization.CultureInfo.InvariantCulture, out printedAmount);
        bool arrowParsed = long.TryParse(
            arrow.Cost, System.Globalization.CultureInfo.InvariantCulture, out arrowAmount);
        return printedParsed && arrowParsed
            && !printed.HasAlternative && !arrow.HasAlternative
            && printed.VariableRequests.Count == 0 && arrow.VariableRequests.Count == 0;
    }

    internal static CostOption? EventPrice(
        World world, Card card, int player, AbilityEffect effect,
        IResourceCardAbilities resourceAbilities)
    {
        if (EffectiveCards.Kind(card, world.Facts) != CardKind.Event)
        {
            return null;
        }

        long cost = CardPayment.CostOf(
            world, world.Facts, world.Seats[player], card).Amount;
        return new CostOption(
            Target: card.ObjectId,
            Cost: cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: Resources.Required(world, card, world.Facts) is { Length: > 0 } required
                ? [required]
                : null,
            Sources: EventGenerators(world, card, player, effect, resourceAbilities),
            DeclarationSensitive: PaidResourceQueries(effect).Any(),
            PreferredResourceTypes: string.Concat(PreferredPaidResourceTypes(effect).Distinct()));
    }

    internal static List<ResourceSource> EventGenerators(
        World world, Card card, int player, AbilityEffect effect,
        IResourceCardAbilities resourceAbilities)
    {
        return CardPayment.Paying(world, world.Facts, world.Seats[player], card)
            .SelectMany(seat => CardPayment.Generators(
                world, world.Facts, seat, resourceAbilities, card))
            .Where(source => source.Effect != card.ObjectId)
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

    internal static List<ResourceSource> PrintedGenerators(
        World world, int player, IResourceCardAbilities resourceAbilities)
    {
        var hand = world.Seats[player].Hand.Cards
            .Select(card => new ResourceSource(
                card.ObjectId,
                Resources.GeneratedBy(card.FaceId, world.Facts)))
            .Where(source => source.Generates.Length > 0);
        return hand
            .Concat(resourceAbilities.PrintedResourceAbilities(world, player))
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

}
