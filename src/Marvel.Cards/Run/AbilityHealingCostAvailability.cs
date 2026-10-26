using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Requires full healing, including damage from a selected resource payment.</summary>
internal sealed class AbilityHealingCostAvailability(
    World world, Card source, int player, AbilityProgram program, IResourceCardAbilities resources)
{
    internal bool CanPay(AbilityCost.Heal heal, IReadOnlyList<int>? paying = null,
        AbilityCost? completeCost = null)
    {
        var target = AbilityPaymentRules.CostTarget(world, source, player, heal.Card);
        if (target is null) return false;
        long required = HealingSteps(completeCost ?? heal)
            .Where(step => AbilityPaymentRules.CostTarget(world, source, player, step.Card) == target)
            .Sum(step => step.Amount);
        var cost = completeCost ?? heal;
        var offers = new AbilityOfferQueries(program, resources);
        if (CurrentlyPayable(target, required, cost)
            && (paying is null || !HasSelectedDamage(paying, offers))) return true;
        var price = EffectiveCards.Kind(source, world.Facts) == CardKind.Event
            ? EventPrice(completeCost)
            : AbilityPaymentPricing.Price(world, source, player, completeCost, resources);
        if (price is null) return false;
        if (paying is not null)
            return ProjectSelected(target, paying, price, offers, cost);

        return Available(target, price, offers, cost);
    }

    private bool HasSelectedDamage(IReadOnlyList<int> paying, AbilityOfferQueries offers) =>
        paying.Any(id => world.Cards[id].Area.Type != DeckType.HandsArea
            && DamageSteps(offers.ResourceAbility(world, player, world.Cards[id])?.Cost).Any());

    private bool CurrentlyPayable(Card target, long required, AbilityCost cost) =>
        DamageSteps(cost).Any() ? Project(target, cost) : target.Damage >= required;

    private bool Available(Card target, CostOption price, AbilityOfferQueries offers, AbilityCost cost)
    {
        // Engine policy: prove a concrete plan using one damage-paying resource
        // ability plus hand resources. Shared or state-changing generator costs
        // need a payment projection before they can establish a healing offer.
        var hand = price.Generators.Where(entry =>
            world.Cards[entry.Effect].Area.Type == DeckType.HandsArea).Select(entry => entry.Effect).ToArray();
        bool unresolved = false;
        foreach (var entry in price.Generators.Where(entry => !hand.Contains(entry.Effect)))
        {
            var generator = world.Cards[entry.Effect];
            var ability = offers.ResourceAbility(world, player, generator);
            if (!DamageSteps(ability?.Cost).Any()) continue;
            if (!Supported(ability!.Cost)) { unresolved = true; continue; }
            int[] plan = [entry.Effect, .. hand];
            if (ResourcePayment.Allocate(price, plan) is null) continue;
            if (ProjectSelected(target, plan, price, offers, cost)) return true;
        }
        if (unresolved || price.Generators.Count(entry => !hand.Contains(entry.Effect)
                && DamageSteps(offers.ResourceAbility(world, player, world.Cards[entry.Effect])?.Cost).Any()) > 1)
            throw Unsupported();
        return false;
    }

    private CostOption EventPrice(AbilityCost? cost)
    {
        var generators = CardPayment.Generators(world, world.Facts, world.Seats[player], resources, source)
            .Where(entry => entry.Effect != source.ObjectId).ToArray();
        return new CostOption(source.ObjectId,
            (CardPayment.CostOf(world, world.Facts, world.Seats[player], source).Amount
                + AbilityPaymentRules.GenericResourceAmount(cost)
                + AbilityPaymentRules.ResourceRequirement(cost, source).Length).ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            Rule: [Resources.Required(world, source, world.Facts)
                + AbilityPaymentRules.ResourceRequirement(cost, source)], Sources: generators);
    }

    private bool ProjectSelected(Card target, IReadOnlyList<int> paying,
        CostOption price, AbilityOfferQueries offers, AbilityCost completeCost)
    {
        var generators = paying.Where(id => world.Cards[id].Area.Type != DeckType.HandsArea).ToArray();
        // A second damage procedure may change replacement admission, prevention
        // uses, defeat or another generator's cost. Do not add independent previews.
        if (generators.Length > 1) throw Unsupported();
        foreach (int id in generators)
        {
            if (!price.Generators.Any(entry => entry.Effect == id)) return false;
            var generator = world.Cards[id];
            var ability = offers.ResourceAbility(world, player, generator);
            if (ability is null) return false;
            if (!Supported(ability.Cost)) throw Unsupported();
            var cost = DamageSteps(ability.Cost).SingleOrDefault();
            if (cost is null)
                continue;
            return Project(target, completeCost, generator, ability.Cost);
        }
        return Project(target, completeCost);
    }

    private bool Project(Card target, AbilityCost cost, Card? generator = null, AbilityCost? generatorCost = null) =>
        new AbilityHealingCostProjection(world, source, player, program, resources, target)
            .CanPay(cost, generator, generatorCost);

    private static bool Supported(AbilityCost? cost) => cost switch
    {
        null => true,
        AbilityCost.Damage => true,
        AbilityCost.Exhaust { Card: AbilityCostCard.Source } => true,
        AbilityCost.Sequence sequence => sequence.Costs.All(Supported)
            && DamageSteps(sequence).Count() <= 1,
        _ => false,
    };

    private static IEnumerable<AbilityCost.Damage> DamageSteps(AbilityCost? cost) => cost switch
    {
        AbilityCost.Damage damage => [damage],
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(DamageSteps),
        _ => [],
    };

    private static IEnumerable<AbilityCost.Heal> HealingSteps(AbilityCost cost) => cost switch
    {
        AbilityCost.Heal heal => [heal],
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(HealingSteps),
        _ => [],
    };

    private static RulesNotImplementedException Unsupported() => new(
        "projecting combined or state-changing resource payments for a healing cost is not implemented");
}
