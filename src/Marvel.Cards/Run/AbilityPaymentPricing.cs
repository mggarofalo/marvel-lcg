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
        if (cost is AbilityCost.InvocationTop)
            return Price(world, card, player, AbilityInvocationPayment.Expand(world, player, cost), resourceAbilities);
        if (cost is AbilityCost.Sequence sequence)
            return SequencePrice(world, card, player, sequence, resourceAbilities);

        if (cost is AbilityCost.SpendX variable)
        {
            long maximum = CardPayment.Generators(
                    world, world.Facts, world.Seats[player], resourceAbilities, abilitySource: card)
                .Sum(source => source.PossibleResources.LongCount(variable.Counts));
            return new CostOption(
                card.ObjectId, "X",
                Sources: CardPayment.Generators(
                    world, world.Facts, world.Seats[player], resourceAbilities, abilitySource: card),
                Variables: [new VariableRequest("X", 1, maximum)],
                Components: [new ResourceCost("X") { RepeatedResource = variable.Resource }]);
        }

        if (cost is AbilityCost.Spend { PrintedOnly: true } printed)
        {
            string printedLetters = printed.Resources;
            return new CostOption(
                Target: card.ObjectId,
                Cost: printedLetters.Length.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Rule: [printedLetters],
                Sources: PrintedGenerators(world, player, resourceAbilities, card),
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
            Cost: spend.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: [letters],
            Sources: CardPayment.Generators(
                world, world.Facts, world.Seats[player], resourceAbilities, abilitySource: card),
            // Engine choice: ordinary single costs use the implicit component
            // so their durable replay-v24 prompt shape remains stable.
            Components: spend.DistinctTypes ? [Component(spend)] : null);
    }

    internal static ResourceCost Component(AbilityCost.Spend spend) =>
        new(spend.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            spend.Resources.Length > 0 ? [spend.Resources] : null, spend.PrintedOnly)
        { DistinctTypes = spend.DistinctTypes };

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
                .GroupBy(source => source.Effect).Select(group => ExpandMask(world, card, resourceAbilities, group.First(), components.Count)).ToList(),
            Components: components);
    }

    internal static CostOption? CombinedPrice(
        World world, Card card, int player, CompiledCardAbility ability,
        IResourceCardAbilities resourceAbilities, ScopedCardCostReduction? reduction = null)
    {
        // rr:non-numerical-variable.1: a printed X is defined by the playing player, which
        // only the card's own spendX cost records.
        if (VariableCardCost.Applies(card.FaceId, world.Facts) && ability.Cost is not AbilityCost.SpendX { Resource: null })
            throw new RulesNotImplementedException($"'{card.FaceId}' costs X but its ability does not define X");
        var printed = EventPrice(world, card, player, ability, resourceAbilities, reduction);
        var arrow = Price(world, card, player, ability.Cost, resourceAbilities);
        if (printed is null || arrow is null)
        {
            return printed ?? arrow;
        }

        if (ability.Cost is AbilityCost.SpendX variable)
            return VariableEventPrice(world, card, resourceAbilities, printed, variable,
                CardPayment.CostOf(world, world.Facts, world.Seats[player], card, null, reduction).VariableReduction);

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
                .Select(group => ExpandMask(world, card, resourceAbilities, group.First(), 2))
                .ToList(),
            // Engine choice: event allocation uses the printed price and one
            // aggregate arrow price, matching the committed payment ledger.
            Components:
            [
                printed.ResourceCosts.Single(),
                new ResourceCost(arrow.Cost, arrow.Rule),
            ]);
    }

    private static CostOption VariableEventPrice(World world, Card card, IResourceCardAbilities resourceAbilities,
        CostOption printed, AbilityCost.SpendX variable, long reduction)
    {
        // Engine choice: display the sum; allocation evaluates its separate components.
        // A reduction left over from the fixed part lowers what X pays, so X may
        // exceed the payable resources by that much (rr:non-numerical-variable.1).
        long amount = long.Parse(printed.Cost, System.Globalization.CultureInfo.InvariantCulture);
        long energy = printed.Generators.Sum(source => source.PossibleResources.LongCount(variable.Counts));
        long total = printed.Generators.Sum(source => (long)source.PossibleResources.Length);
        string paid = reduction > 0
            ? "X-" + reduction.ToString(System.Globalization.CultureInfo.InvariantCulture) : "X";
        return printed with
        {
            Cost = printed.Cost + "+" + paid,
            Variables = [new VariableRequest("X", 1, checked(Math.Min(energy, Math.Max(0, total - amount)) + reduction))],
            Sources = [.. printed.Generators.Select(source => ExpandMask(world, card, resourceAbilities, source, 2))],
            Components = [printed.ResourceCosts.Single(),
                new ResourceCost(paid) { RepeatedResource = variable.Resource }],
        };
    }

    private static ResourceSource ExpandMask(World world, Card owner, IResourceCardAbilities abilities,
        ResourceSource source, int components)
    {
        if (source.ConditionalBonus is null || source.Effect < 0 || source.Effect >= world.Cards.Count
            || world.Cards[source.Effect].Area.Type != DeckType.HandsArea) return source;
        if (components > 64) throw new RulesNotImplementedException("too many conditional cost components");
        var card = world.Cards[source.Effect];
        if (abilities.QuoteHandResources(world, card, owner) != source) return source;
        ulong mask = components == 64 ? ulong.MaxValue : (1UL << components) - 1;
        return abilities.QuoteHandResources(world, card, owner, mask);
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
        World world, Card card, int player, CompiledCardAbility ability,
        IResourceCardAbilities resourceAbilities, ScopedCardCostReduction? reduction = null)
    {
        var effect = ability.Effect;
        if (EffectiveCards.Kind(card, world.Facts) != CardKind.Event)
        {
            return null;
        }

        long cost = CardPayment.CostOf(
            world, world.Facts, world.Seats[player], card, attachmentHost: null, reduction: reduction).Amount;
        return new CostOption(
            Target: card.ObjectId,
            Cost: cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: PrintedRequirement(world, card, ability, cost) is { Length: > 0 } required
                ? [required]
                : null,
            Sources: EventGenerators(world, card, player, effect, resourceAbilities),
            DeclarationSensitive: PaidResourceQueries(effect).Any(),
            PreferredResourceTypes: string.Concat(PreferredPaidResourceTypes(effect).Distinct()),
            Components: ability.PrintedCostResource is { } resource
                ? [new ResourceCost(cost.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    { RepeatedResource = resource }] : null);
    }

    internal static string PrintedRequirement(
        World world, Card card, CompiledCardAbility ability, long adjusted) =>
        ability.PrintedCostResource is { } resource
            ? new string(resource, checked((int)adjusted))
            : Resources.Required(world, card, world.Facts);

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
        World world, int player, IResourceCardAbilities resourceAbilities, Card source)
    {
        var hand = world.Seats[player].Hand.Cards
            .Select(card => new ResourceSource(
                card.ObjectId,
                Resources.GeneratedBy(card.FaceId, world.Facts)))
            .Where(source => source.Generates.Length > 0);
        return hand
            .Concat(resourceAbilities.PrintedResourceAbilities(world, player)
                .Where(generator => ResourceSourceEligibility.Allows(world, world.Facts, generator, source)))
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

}
