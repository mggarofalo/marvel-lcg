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
        IReadOnlyDictionary<string, long>? values, AbilityCost? completeCost = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
    {
        if (cost is null)
        {
            return;
        }

        IReadOnlyList<AbilityCost> steps = AbilityCostOrder.Flatten(cost).ToArray();
        ValidateCombinedCosts(steps, paying, chosen);
        var spends = steps.OfType<AbilityCost.Spend>().ToList();
        ValidateResourceSpend(
            spends, paying, chosen, allocations ?? []);

        foreach (var step in steps.Where(
                     step => step is not AbilityCost.Spend))
        {
            ValidateNonResourceCost(
                step, paying, chosen, values, completeCost ?? cost, allocations ?? []);
        }
    }

    private void ValidateCombinedCosts(
        IReadOnlyList<AbilityCost> steps, IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
    {
        if (steps.Any(step => step is AbilityCost.DiscardFromHand) && paying.Intersect(chosen).Any())
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' cannot discard the same hand card for resources and an arrow cost");
        if (!AbilityRandomHandCost.CanPayTogether(world, source, player, steps))
            throw new RulesNotImplementedException("the random hand-discard cost cannot be paid in full");
        if (!AbilityDeckCosts.CanPayTogether(world, player, steps))
            throw new RulesNotImplementedException("the combined deck costs cannot be paid in full");
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
    }

    private void ValidateResourceSpend(
        List<AbilityCost.Spend> spends, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyList<ResourceAllocation> allocations)
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
            ? PrintedGenerators(world, player, resourceAbilities, source)
            : CardPayment.Generators(
                world, world.Facts, world.Seats[player], resourceAbilities, abilitySource: source).ToList();
        if (paying.Any(id => generators.All(candidate => candidate.Effect != id)))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' names a resource source that is not available");
        string generated = string.Concat(generators.Where(generator => paying.Contains(generator.Effect))
            .Select(generator => ResourcePayment.GeneratedBy(generator, allocations)));
        string required = string.Concat(spends.Select(step => step.Resources));
        bool pays = printed
            ? Resources.PaysPrinted(generated, spends.Sum(step => step.Amount), required)
            : Resources.Pays(generated, spends.Sum(step => step.Amount), required);
        if (!pays)
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has simultaneous resource costs requiring '{required}' and the payment generates '{generated}'");
    }

    private void ValidateNonResourceCost(
        AbilityCost step, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, AbilityCost? completeCost,
        IReadOnlyList<ResourceAllocation> allocations)
    {
        if (ValidateChoiceCost(step, paying, chosen)) return;
        if (step is AbilityCost.Heal heal)
        {
            ValidateHealing(heal, paying, completeCost);
            return;
        }
        if (step is AbilityCost.SpendEnergy)
            ValidateEnergySpend(step, paying, values, allocations);
        else if (step is AbilityCost.DiscardFromHand discard)
            ValidateHandDiscard(discard, chosen);
        else if (step is AbilityCost.ExhaustChosen exhaust)
            ValidateChosenExhaust(exhaust, chosen);
        else if (!Payable(world, source, player, step, program, resourceAbilities))
            throw new RulesNotImplementedException($"'{source.FaceId}' cannot pay its compiled cost");
    }

    private bool ValidateChoiceCost(AbilityCost step, IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
    {
        if (step is AbilityCost.ChooseScheme)
        {
            AbilitySchemeChoice.Validate(world, source, player, chosen);
            return true;
        }
        if (step is not AbilityCost.ChooseFromDiscard choice) return false;
        if (paying.Count != 0 || chosen.Count != 1
            || !AbilityCostSelection.DiscardCandidates(world, player, choice).Any(card => card.ObjectId == chosen[0]))
            throw new RulesNotImplementedException("chooseFromDiscard requires one current own-discard card and no resource payment");
        return true;
    }

    private void ValidateHealing(AbilityCost.Heal heal, IReadOnlyList<int> paying, AbilityCost? completeCost)
    {
        if (!new AbilityHealingCostAvailability(world, source, player, program, resourceAbilities)
                .CanPay(heal, paying, completeCost))
            throw new RulesNotImplementedException($"'{source.FaceId}' cannot pay its full healing cost");
    }

    private void ValidateEnergySpend(
        AbilityCost step, IReadOnlyList<int> paying, IReadOnlyDictionary<string, long>? values,
        IReadOnlyList<ResourceAllocation> allocations)
    {
        long x = DefinedVariable(values, "X", source);
        var request = Price(world, source, player, step, resourceAbilities)!
            .VariableRequests.Single(variable => variable.Name == "X");
        if (!request.Allows(x))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' defines X as {x}, outside the offered range {request.Min}..{request.Max}");
        var generators = CardPayment.Generators(
            world, world.Facts, world.Seats[player], resourceAbilities, abilitySource: source).ToList();
        if (paying.Count == 0 || paying.Distinct().Count() != paying.Count
            || paying.Any(id => generators.All(candidate => candidate.Effect != id)))
            throw new RulesNotImplementedException($"'{source.FaceId}' names an invalid generator for X");
        string generated = string.Concat(generators.Where(generator => paying.Contains(generator.Effect))
            .Select(generator => ResourcePayment.GeneratedBy(generator, allocations)));
        if (!Resources.Pays(generated, x, new string(Resources.Energy, checked((int)x))))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' defines X as {x}, but the payment generates '{generated}'");
    }

    private void ValidateHandDiscard(
        AbilityCost.DiscardFromHand discard, IReadOnlyList<int> chosen)
    {
        var candidates = AbilityCostSelection.HandCandidates(world, source, player, discard);
        var (minimum, maximum) = AbilityCostSelection.Range(discard.Range, candidates.Count);
        if (chosen.Count < minimum || chosen.Count > maximum
            || chosen.Distinct().Count() != chosen.Count)
        {
            string required = minimum == maximum ? $"{minimum}" : $"{minimum}..{maximum}";
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' costs {required} card(s) from hand and {chosen.Count} were chosen; rr:initiating-abilities.step.5 aborts without paying");
        }
        foreach (int id in chosen)
            if (!candidates.Any(card => card.ObjectId == id))
                throw new RulesNotImplementedException(
                    $"card {id} is not eligible for {world.Seats[player].Name}'s hand-discard cost");
    }

    private void ValidateChosenExhaust(
        AbilityCost.ExhaustChosen exhaust, IReadOnlyList<int> chosen)
    {
        var legal = AbilityCostSelection.Choices(world, player, exhaust)
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
