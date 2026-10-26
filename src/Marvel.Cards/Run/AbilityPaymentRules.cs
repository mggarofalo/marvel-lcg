using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityCostSelection;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Cost eligibility, pricing and complete payment-answer validation.</summary>
/// <remarks>Reads current payment facts; owns no event sink or execution continuation.</remarks>
internal static class AbilityPaymentRules
{
    /// <summary>
    /// Whether an ability's cost can be paid — <c>rr:initiating-abilities.step.3</c>.
    /// </summary>
    /// <remarks>
    /// Asked before the ability is offered, because "the player's ability to pay
    /// them" is step 3 and step 5 aborts "without paying any costs" — so an
    /// ability that would abort is not an offer, it is a trap. An exhausted card
    /// cannot pay a cost of exhausting itself: <c>rr:exhausted.2</c>.
    /// </remarks>
    internal static bool Payable(
        World world, Card card, int player, AbilityCost? cost,
        AbilityProgram program, IResourceCardAbilities resourceAbilities) =>
        cost switch
        {
            null => true,
            AbilityCost.ReturnSourceToHand => AbilityPhysicalPayment.CanReturnToHand(world, card),
            AbilityCost.ReturnTitledToHand named => NamedAbilityCost.Target(world, player, named.Title) is { } returning
                && AbilityPhysicalPayment.CanReturnToHand(world, returning),
            AbilityCost.DiscardTop discard => AbilityDeckCosts.CanDiscard(world, player, discard),
            // Hidden deck identities cannot establish whether this uncertain cost succeeds.
            AbilityCost.DiscardUntilMinionEngaged or AbilityCost.DealEncounterToYou => player >= 0 && AbilityDeckCosts.EncounterAvailable(world) > 0,
            AbilityCost.Sequence sequence => SequencePayable(
                world, card, player, sequence, program, resourceAbilities),
            AbilityCost.ExhaustTitled named => NamedAbilityCost.Target(world, player, named.Title)?.Ready == true,
            AbilityCost.Exhaust exhaust => CostTarget(world, card, player, exhaust.Card)?.Ready == true,
            AbilityCost.ConsumeCampaignCard consume => CostTarget(world, card, player, consume.Card) is { } reward
                && AbilityPhysicalPayment.CanConsumeCampaign(world, card, reward),
            AbilityCost.Discard discard => CostTarget(world, card, player, discard.Card) is not null,
            AbilityCost.RemoveCounters removal => CostTarget(world, card, player, removal.Card) is { } counterCard
                && CounterKeyForRemoval(
                    counterCard, removal.Counter, removal.Count) is not null,

            // Every other cost is somebody's, and an ability offered to every
            // seat at once has not said whose. `AbilityTrigger.Player` is where
            // a card that means one seat says so.
            _ when player < 0 => throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a cost and is offered to every player, "
                + "so there is no hand to price it against"),

            // Asked of the whole hand, which is the right question rather than
            // an approximation: `rr:cost.4` permits generating beyond the cost,
            // so if everything together cannot pay then no choice among them
            // can, and if it can then spending it all is a payment.
            AbilityCost.Spend spend => ResourcePayable(world, card, player, spend, resourceAbilities),
            AbilityCost.SpendEnergy => Resources.Pays(
                string.Concat(CardPayment.Generators(
                        world, world.Facts, world.Seats[player], resourceAbilities, abilitySource: card)
                    .SelectMany(source => source.Generates)),
                1,
                "Y"),

            // "Discard **a card** from your hand" -- `rr:cost.3` spends
            // resources by discarding cards, and this is the other thing a
            // discard can be: the card is the cost and what it would have
            // generated is not read at all. So the question is a count and not
            // a sum, and a card with no printed `RES` pays it.
            AbilityCost.RandomDiscardFromHand random => AbilityRandomHandCost.CanPay(world, card, player, random),
            AbilityCost.ChooseFromDiscard choice => AbilityCostSelection.DiscardCandidates(world, player, choice).Count > 0,
            AbilityCost.DiscardFromHand discard => AbilityCostSelection.HandCandidates(world, card, player).Count
                >= AbilityCostSelection.Range(discard.Range, int.MaxValue).Min,
            AbilityCost.ExhaustChosen exhaust => AbilityCostSelection.Choices(world, player, exhaust)
                .Count(card => card.Ready) >= AbilityCostSelection.Range(exhaust.Range, int.MaxValue).Min,

            AbilityCost.Heal heal => CostTarget(world, card, player, heal.Card) is { Damage: > 0 },

            AbilityCost.Damage damage => CostTarget(
                    world, card, player, damage.Card) is { } takingTarget
                && AbilityProgramQueries.CanTakeDamage(
                    world, program, takingTarget, card)
                // `rr:cost.12`: "that cost is not considered paid unless all
                // of that damage was taken." Tough necessarily prevents the
                // next instance, so this cost cannot be paid at initiation.
                && (!damage.MustTakeAll || !Statuses.Has(world, takingTarget, Statuses.Tough)),

            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' has an unknown compiled cost"),
        };

    /// <summary>Whether a mandatory cost can be paid without asking a player.</summary>
    /// <remarks>
    /// A forced ability is not optional, but its arrow cost is still paid at
    /// <c>rr:initiating-abilities.step.5</c>. A cost that identifies its own
    /// payment, such as “discard this card,” needs no decision. Resource and
    /// variable-card costs do, so they remain explicitly unimplemented until
    /// the timing window can carry that mandatory payment prompt.
    /// </remarks>
    internal static bool MandatoryCostIsAutomatic(AbilityCost cost) => cost switch
    {
        AbilityCost.DiscardUntilMinionEngaged or AbilityCost.DiscardTop or AbilityCost.DealEncounterToYou => true,
        AbilityCost.Sequence sequence => sequence.Costs.All(MandatoryCostIsAutomatic),
        AbilityCost.ReturnSourceToHand or AbilityCost.ReturnTitledToHand or AbilityCost.ConsumeCampaignCard
            or AbilityCost.Discard or AbilityCost.Exhaust or AbilityCost.ExhaustTitled or AbilityCost.RemoveCounters => true,
        _ => false,
    };

    internal static bool SequencePayable(
        World world, Card card, int player, AbilityCost.Sequence cost,
        AbilityProgram program, IResourceCardAbilities resourceAbilities)
    {
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
                world, card, player, step, program, resourceAbilities));
    }

    private static bool ResourcePayable(World world, Card card, int player,
        AbilityCost.Spend spend, IResourceCardAbilities resourceAbilities)
    {
        var price = AbilityPaymentPricing.Price(world, card, player, spend, resourceAbilities)!;
        return ResourcePayment.Allocate(price, price.Generators.Select(source => source.Effect).ToArray()) is not null;
    }

    private static bool PhysicalCostsPayable(World world, Card card, int player, AbilityCost.Sequence cost) =>
        AbilityRandomHandCost.CanPayTogether(world, card, player, cost.Costs)
        && CounterCostsPayable(world, card, player, cost)
        && AbilityDeckCosts.CanPayTogether(world, player, cost.Costs);

    internal static bool CounterCostsPayable(
        World world, Card card, int player, AbilityCost? cost)
    {
        if (cost is null)
        {
            return true;
        }

        var counterCosts = new Dictionary<(int Card, string Type), long>();
        var counterCards = new Dictionary<int, Card>();
        foreach (var removal in CounterCostSteps(cost))
        {
            if (CostTarget(world, card, player, removal.Card) is not { } target)
            {
                return false;
            }
            var key = (target.ObjectId, removal.Counter);
            counterCards[target.ObjectId] = target;
            counterCosts[key] = checked(
                counterCosts.GetValueOrDefault(key) + removal.Count);
        }
        foreach (var byCard in counterCosts.GroupBy(cost => cost.Key.Card))
        {
            if (byCard.Any(cost => cost.Key.Type == "allPurpose")
                && byCard.Count() > 1)
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' mixes an all-purpose counter cost with a typed "
                    + "counter cost on the same card");
            }
        }
        if (counterCosts.Any(cost => CounterKeyForRemoval(
                counterCards[cost.Key.Card], cost.Key.Type, cost.Value) is null))
        {
            return false;
        }
        return true;
    }

    internal static IEnumerable<AbilityCost.RemoveCounters> CounterCostSteps(AbilityCost cost)
    {
        if (cost is AbilityCost.RemoveCounters removal)
        {
            yield return removal;
            yield break;
        }
        if (cost is not AbilityCost.Sequence sequence)
        {
            yield break;
        }
        foreach (var step in sequence.Costs)
        {
            foreach (var counterCost in CounterCostSteps(step))
            {
                yield return counterCost;
            }
        }
    }

    internal static Card? CostTarget(World world, Card source, int player, AbilityCostCard value) => value switch
    {
        AbilityCostCard.Source => source,
        AbilityCostCard.Identity => player >= 0 ? world.Seats[player].IdentityCard : null,
        _ => throw new InvalidOperationException("Unknown compiled cost card"),
    };

    internal static bool EventPayable(
        World world, Card card, int player, CompiledCardAbility ability,
        IResourceCardAbilities resourceAbilities, IReadOnlyList<int>? discarded = null, int? discardCount = null)
    {
        if (EffectiveCards.Kind(card, world.Facts) != CardKind.Event)
        {
            return true;
        }

        AttachedUpgradeCostBound.Validate(world, card, player, ability.Cost, []);
        if (!Resources.HasPlayableCost(card.FaceId, world.Facts))
        {
            return false;
        }

        if (ability.Cost is AbilityCost.SpendEnergy)
        {
            var price = AbilityPaymentPricing.CombinedPrice(world, card, player, ability, resourceAbilities)!;
            var sources = price.Generators.Where(source => discarded is null || !discarded.Contains(source.Effect))
                .Select(source => source.Effect).ToArray();
            return ResourcePayment.Allocate(price, sources, new Dictionary<string, long> { ["X"] = 1 }) is not null;
        }

        long cost = CardPayment.CostOf(
            world, world.Facts, world.Seats[player], card).Amount;
        string required = Resources.Required(world, card, world.Facts)
            + ResourceRequirement(ability.Cost, card);
        cost += required.Length
            - Resources.Required(world, card, world.Facts).Length + GenericResourceAmount(ability.Cost);
        var generators = AbilityPaymentPricing.EventGenerators(world, card, player, ability.Effect, resourceAbilities);
        if (discarded is not null)
            return Resources.Pays(new string([.. generators
                .Where(source => !discarded.Contains(source.Effect)).SelectMany(source => source.Generates)]), cost, required);
        return AbilityHandDiscardAvailability.CanPay(ability.Cost,
            AbilityCostSelection.HandCandidates(world, card, player), generators, cost, required, discardCount);
    }

    internal static bool EventPaysResources(World world, Card card, AbilityCost? cost) =>
        EffectiveCards.Kind(card, world.Facts) == CardKind.Event
        && (cost is AbilityCost.SpendEnergy
            || ResourceRequirement(cost, card).Length + GenericResourceAmount(cost) > 0);

    internal static int GenericResourceAmount(AbilityCost? cost) => cost switch
    {
        AbilityCost.Spend spend => spend.Generic,
        AbilityCost.Sequence sequence => sequence.Costs.Sum(GenericResourceAmount),
        _ => 0,
    };

    internal static string ResourceRequirement(AbilityCost? cost, Card card) => cost switch
    {
        null => string.Empty,
        AbilityCost.Spend { DistinctTypes: true } => throw new RulesNotImplementedException(
            "event printed costs combined with a distinct-type arrow cost are not implemented"),
        AbilityCost.Spend { PrintedOnly: false } spend => spend.Resources,
        AbilityCost.Spend { PrintedOnly: true } => throw new RulesNotImplementedException(
            $"event '{card.FaceId}' combines its printed card cost with a printed-resource "
            + "arrow cost, whose allocation is not implemented"),
        AbilityCost.Sequence sequence => string.Concat(sequence.Costs
            .Select(step => step switch
            {
                AbilityCost.Spend { DistinctTypes: true } => throw new RulesNotImplementedException(
                    "event printed costs combined with a distinct-type arrow cost are not implemented"),
                AbilityCost.Spend { PrintedOnly: false } spend => spend.Resources,
                AbilityCost.Spend { PrintedOnly: true } => throw new RulesNotImplementedException(
                    $"event '{card.FaceId}' combines its printed card cost with a "
                    + "printed-resource arrow cost, whose allocation is not implemented"),
                _ => string.Empty,
            })),
        AbilityCost.SpendEnergy => throw new RulesNotImplementedException(
            $"event '{card.FaceId}' combines a printed cost with a variable X cost"),
        _ => string.Empty,
    };
}
