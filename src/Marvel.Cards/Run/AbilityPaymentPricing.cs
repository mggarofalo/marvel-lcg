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
                card.ObjectId, "X", ["Y"],
                Sources: CardPayment.Generators(
                    world, world.Facts, world.Seats[player], resourceAbilities),
                Variables: [new VariableRequest("X", 1, maximum)]);
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
        if (world.Facts.Kind(card.FaceId) != CardKind.Event)
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
            DeclarationSensitive: PaidResourceQueries(effect).Any());
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

    /// <summary>Validates every selected cost before any simultaneous cost is paid.</summary>
    internal static void ValidatePayment(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        World world, Card source, int player, AbilityProgram program,
        IResourceCardAbilities resourceAbilities)
        => ValidatePayment(
            cost, paying, chosen, values: null, world, source, player,
            program, resourceAbilities);

    internal static void ValidatePayment(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, World world, Card source, int player,
        AbilityProgram program, IResourceCardAbilities resourceAbilities)
    {
        if (cost is null)
        {
            return;
        }

        IReadOnlyList<AbilityCost> steps = cost is AbilityCost.Sequence sequence ? sequence.Costs : [cost];
        if (world.Facts.Kind(source.FaceId) == CardKind.Event
            && steps.Any(step => step is AbilityCost.Damage { MustTakeAll: true }))
        {
            // Playing an event pays its printed resource price through
            // AbilityEventPayment. Rolling that payment back after damage prevention is a
            // transaction the engine does not yet represent, so refuse it
            // before the event or a generator moves.
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' is an event with a take-damage cost; "
                + "atomic printed and damage payment is not implemented");
        }
        var spends = steps.OfType<AbilityCost.Spend>().ToList();
        ValidateResourceSpend(
            spends, paying, chosen, world, source, player, resourceAbilities);

        foreach (var step in steps.Where(
                     step => step is not AbilityCost.Spend))
        {
            ValidateNonResourceCost(
                step, paying, chosen, values, world, source, player,
                program, resourceAbilities);
        }
    }

    private static void ValidateResourceSpend(
        List<AbilityCost.Spend> spends, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, World world, Card source, int player,
        IResourceCardAbilities resourceAbilities)
    {
        if (spends.Count == 0) return;
        if (paying.Distinct().Count() != paying.Count || paying.Intersect(chosen).Any())
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' names a generator more than once across its costs");
        bool printed = spends.All(step => step.PrintedOnly);
        if (!printed && spends.Any(step => step.PrintedOnly))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' mixes printed and ordinary simultaneous resource costs, whose allocation is not implemented");
        var generators = printed
            ? PrintedGenerators(world, player, resourceAbilities)
            : CardPayment.Generators(
                world, world.Facts, world.Seats[player], resourceAbilities).ToList();
        if (paying.Any(id => generators.All(candidate => candidate.Effect != id)))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' names a resource source that is not available");
        string generated = GeneratedResources(generators, paying);
        string required = string.Concat(spends.Select(step => step.Resources));
        bool pays = printed
            ? Resources.PaysPrinted(generated, required.Length, required)
            : Resources.Pays(generated, required.Length, required);
        if (!pays)
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has simultaneous resource costs requiring '{required}' and the payment generates '{generated}'");
    }

    private static string GeneratedResources(
        IEnumerable<ResourceSource> generators, IReadOnlyList<int> paying)
    {
        var selected = paying.ToHashSet();
        return string.Concat(generators
            .Where(source => selected.Contains(source.Effect))
            .Select(source => source.Generates));
    }

    private static void ValidateNonResourceCost(
        AbilityCost step, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, World world, Card source, int player,
        AbilityProgram program, IResourceCardAbilities resourceAbilities)
    {
        if (step is AbilityCost.SpendEnergy)
            ValidateEnergySpend(step, paying, values, world, source, player, resourceAbilities);
        else if (step is AbilityCost.DiscardFromHand discard)
            ValidateHandDiscard(discard, chosen, world, source, player);
        else if (step is AbilityCost.ExhaustChosen exhaust)
            ValidateChosenExhaust(exhaust, chosen, world, source, player);
        else if (!Payable(world, source, player, step, program, resourceAbilities))
            throw new RulesNotImplementedException($"'{source.FaceId}' cannot pay its compiled cost");
    }

    private static void ValidateEnergySpend(
        AbilityCost step, IReadOnlyList<int> paying,
        IReadOnlyDictionary<string, long>? values, World world, Card source, int player,
        IResourceCardAbilities resourceAbilities)
    {
        long x = DefinedVariable(values, "X", source);
        var request = Price(world, source, player, step, resourceAbilities)!
            .VariableRequests.Single(variable => variable.Name == "X");
        if (!request.Allows(x))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' defines X as {x}, outside the offered range {request.Min}..{request.Max}");
        var generators = CardPayment.Generators(
            world, world.Facts, world.Seats[player], resourceAbilities).ToList();
        if (paying.Count == 0 || paying.Distinct().Count() != paying.Count
            || paying.Any(id => generators.All(candidate => candidate.Effect != id)))
            throw new RulesNotImplementedException($"'{source.FaceId}' names an invalid generator for X");
        string generated = GeneratedResources(generators, paying);
        if (!Resources.Pays(generated, x, new string(Resources.Energy, checked((int)x))))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' defines X as {x}, but the payment generates '{generated}'");
    }

    private static void ValidateHandDiscard(
        AbilityCost.DiscardFromHand discard, IReadOnlyList<int> chosen,
        World world, Card source, int player)
    {
        var hand = world.Seats[player].Hand;
        var (minimum, maximum) = AbilityCostSelection.Range(discard.Range, hand.Cards.Count);
        if (chosen.Count < minimum || chosen.Count > maximum
            || chosen.Distinct().Count() != chosen.Count)
        {
            string required = minimum == maximum ? $"{minimum}" : $"{minimum}..{maximum}";
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' costs {required} card(s) from hand and {chosen.Count} were chosen; rr:initiating-abilities.step.5 aborts without paying");
        }
        foreach (int id in chosen)
            if (world.Cards[id].Area != hand)
                throw new RulesNotImplementedException(
                    $"card {id} is not in {world.Seats[player].Name}'s hand and cannot be discarded from it");
    }

    private static void ValidateChosenExhaust(
        AbilityCost.ExhaustChosen exhaust, IReadOnlyList<int> chosen,
        World world, Card source, int player)
    {
        var legal = AbilityCostSelection.Choices(world, player, exhaust.From)
            .Where(card => card.Ready).Select(card => card.ObjectId).ToHashSet();
        var range = AbilityCostSelection.Range(exhaust.Range, legal.Count);
        if (chosen.Count < range.Min || chosen.Count > range.Max
            || chosen.Distinct().Count() != chosen.Count
            || chosen.Any(id => !legal.Contains(id)))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires {range.Min}..{range.Max} legal cards to exhaust and {chosen.Count} were supplied");
    }

    internal static long DefinedVariable(
        IReadOnlyDictionary<string, long>? values, string name, Card source) =>
        values is not null && values.TryGetValue(name, out long value)
            ? value
            : throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires an explicit value for {name}");

}
