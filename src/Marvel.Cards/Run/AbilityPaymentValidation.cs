using static Marvel.Cards.Run.AbilityPaymentRules;
using static Marvel.Cards.Run.AbilityPaymentPricing;
using static Marvel.Cards.Run.AbilityCostSelection;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Validates the complete selected cost before any component is paid.</summary>
internal sealed class AbilityPaymentValidation(
    World world, Card source, int player, AbilityProgram program, IResourceCardAbilities resourceAbilities)
{
    internal void Validate(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values)
    {
        if (cost is null)
        {
            return;
        }

        IReadOnlyList<AbilityCost> steps = cost is AbilityCost.Sequence sequence ? sequence.Costs : [cost];
        if (EffectiveCards.Kind(source, world.Facts) == CardKind.Event
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
            spends, paying, chosen);

        foreach (var step in steps.Where(
                     step => step is not AbilityCost.Spend))
        {
            ValidateNonResourceCost(
                step, paying, chosen, values);
        }
    }

    private void ValidateResourceSpend(
        List<AbilityCost.Spend> spends, IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
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

    private void ValidateNonResourceCost(
        AbilityCost step, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values)
    {
        if (step is AbilityCost.SpendEnergy)
            ValidateEnergySpend(step, paying, values);
        else if (step is AbilityCost.DiscardFromHand discard)
            ValidateHandDiscard(discard, chosen);
        else if (step is AbilityCost.ExhaustChosen exhaust)
            ValidateChosenExhaust(exhaust, chosen);
        else if (!Payable(world, source, player, step, program, resourceAbilities))
            throw new RulesNotImplementedException($"'{source.FaceId}' cannot pay its compiled cost");
    }

    private void ValidateEnergySpend(
        AbilityCost step, IReadOnlyList<int> paying, IReadOnlyDictionary<string, long>? values)
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

    private void ValidateHandDiscard(
        AbilityCost.DiscardFromHand discard, IReadOnlyList<int> chosen)
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

    private void ValidateChosenExhaust(
        AbilityCost.ExhaustChosen exhaust, IReadOnlyList<int> chosen)
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
