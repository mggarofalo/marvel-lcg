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
        if (target.Damage >= required) return true;
        heal = heal with { Amount = required };
        if (DamageSteps(completeCost).Any()) throw Unsupported();
        var price = EffectiveCards.Kind(source, world.Facts) == CardKind.Event
            ? EventPrice(completeCost)
            : AbilityPaymentPricing.Price(world, source, player, completeCost, resources);
        if (price is null) return false;
        var offers = new AbilityOfferQueries(program, resources);
        if (paying is not null)
            return ProjectSelected(heal, target, paying, price, offers);

        return Available(heal, target, price, offers);
    }

    private bool Available(AbilityCost.Heal heal, Card target, CostOption price, AbilityOfferQueries offers)
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
            if (ProjectSelected(heal, target, plan, price, offers)) return true;
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

    private bool ProjectSelected(AbilityCost.Heal heal, Card target, IReadOnlyList<int> paying,
        CostOption price, AbilityOfferQueries offers)
    {
        var generators = paying.Where(id => world.Cards[id].Area.Type != DeckType.HandsArea).ToArray();
        // A second damage procedure may change replacement admission, prevention
        // uses, defeat or another generator's cost. Do not add independent previews.
        if (generators.Length > 1) throw Unsupported();
        long damage = target.Damage;
        foreach (int id in generators)
        {
            if (!price.Generators.Any(entry => entry.Effect == id)) return false;
            var generator = world.Cards[id];
            var ability = offers.ResourceAbility(world, player, generator);
            if (ability is null) return false;
            if (!Supported(ability.Cost)) throw Unsupported();
            var cost = DamageSteps(ability.Cost).SingleOrDefault();
            if (cost is null || AbilityPaymentRules.CostTarget(world, generator, player, cost.Card) != target)
                continue;
            long taken = ProjectDamage(target, generator, cost);
            damage = AbilityAmounts.SaturatingSum(damage, [taken]);
        }
        return damage >= heal.Amount;
    }

    private long ProjectDamage(Card target, Card generator, AbilityCost.Damage cost)
    {
        if (!AbilityProgramQueries.CanTakeDamage(world, program, target, generator)) return 0;
        long amount = cost.MustTakeAll ? cost.Amount : AbilityAmounts.SaturatingSum(cost.Amount,
            [AbilityEventModifiers.Amount(world, generator, "eventDamage")]);
        var replacement = new AbilityDamageProjection(program, resources)
            .PreviewDamageReplacement(world, target, generator, amount);
        if (replacement.Result is not RuleProjection<long>.Known known) throw Unsupported();
        var assignment = DamageAssignment.AfterReplacement(known.Value,
            Statuses.Has(world, target, Statuses.Tough));
        long taken = assignment.Taken;
        foreach (var prevention in DamagePrevention.Active(world, target))
            taken = DamagePrevention.After(taken, prevention.Amount);
        if (cost.MustTakeAll && taken != cost.Amount) return 0;
        // Resource generation settles its own defeat before the outer heal.
        if (AbilityAmounts.SaturatingSum(target.Damage, [taken]) >= DamagePlacement.Health(world, world.Facts, target))
            throw Unsupported();
        return taken;
    }

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
