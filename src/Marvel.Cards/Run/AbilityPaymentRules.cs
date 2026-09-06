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
        AbilityProgram? program = null) =>
        cost switch
        {
            null => true,
            AbilityCost.Sequence sequence => SequencePayable(
                world, card, player, sequence, program),
            AbilityCost.Exhaust exhaust => CostTarget(world, card, player, exhaust.Card)?.Ready == true,
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
            AbilityCost.Spend { PrintedOnly: false } spend => Resources.Pays(
                string.Concat(CardPlay.Generators(world, world.Facts, world.Seats[player])
                    .SelectMany(source => source.Generates)),
                spend.Resources.Length,
                spend.Resources),
            AbilityCost.Spend { PrintedOnly: true } spend => Resources.PaysPrinted(
                string.Concat(PrintedGenerators(world, player)
                    .SelectMany(source => source.Generates)),
                spend.Resources.Length,
                spend.Resources),
            AbilityCost.SpendEnergy => Resources.Pays(
                string.Concat(CardPlay.Generators(world, world.Facts, world.Seats[player])
                    .SelectMany(source => source.Generates)),
                1,
                "Y"),

            // "Discard **a card** from your hand" -- `rr:cost.3` spends
            // resources by discarding cards, and this is the other thing a
            // discard can be: the card is the cost and what it would have
            // generated is not read at all. So the question is a count and not
            // a sum, and a card with no printed `RES` pays it.
            AbilityCost.DiscardFromHand discard => world.Seats[player].Hand.Cards.Count
                >= AbilityCostSelection.Range(discard.Range, int.MaxValue).Min,
            AbilityCost.ExhaustChosen exhaust => AbilityCostSelection.Choices(world, player, exhaust.From)
                .Count(card => card.Ready) >= AbilityCostSelection.Range(exhaust.Range, int.MaxValue).Min,

            AbilityCost.Heal heal => CostTarget(world, card, player, heal.Card) is { Damage: > 0 },

            AbilityCost.Damage damage => CostTarget(
                    world, card, player, damage.Card) is { } takingTarget
                && (program is not null
                    ? AbilityProgramQueries.CanTakeDamage(
                        world, program, takingTarget, card)
                    : world.Abilities.CanTakeDamage(world, takingTarget, card))
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
        AbilityCost.Sequence sequence => sequence.Costs.All(MandatoryCostIsAutomatic),
        AbilityCost.Discard or AbilityCost.Exhaust or AbilityCost.RemoveCounters => true,
        _ => false,
    };

    internal static bool SequencePayable(
        World world, Card card, int player, AbilityCost.Sequence cost,
        AbilityProgram? program = null)
    {
        var steps = cost.Costs;
        if (!CounterCostsPayable(world, card, player, cost))
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

            string required = string.Concat(spends.Select(step => step.Resources));
            string pool = string.Concat((printed
                    ? PrintedGenerators(world, player)
                    : CardPlay.Generators(world, world.Facts, world.Seats[player]))
                .SelectMany(source => source.Generates));
            bool pays = printed
                ? Resources.PaysPrinted(pool, required.Length, required)
                : Resources.Pays(pool, required.Length, required);
            if (!pays)
            {
                return false;
            }
        }

        return steps.Where(step => step is not (AbilityCost.Spend or AbilityCost.RemoveCounters))
            .All(step => Payable(world, card, player, step, program));
    }

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
        World world, Card card, int player, CompiledCardAbility ability)
    {
        if (world.Facts.Kind(card.FaceId) != CardKind.Event)
        {
            return true;
        }

        if (!Resources.HasPlayableCost(card.FaceId, world.Facts))
        {
            return false;
        }

        long cost = CardPlay.CostOf(
            world, world.Facts, world.Seats[player], card).Amount;
        string required = Resources.Required(world, card, world.Facts)
            + ResourceRequirement(ability.Cost, card);
        cost += required.Length
            - Resources.Required(world, card, world.Facts).Length;
        string pool = string.Concat(EventGenerators(world, card, player, ability.Effect)
            .SelectMany(source => source.Generates));
        return Resources.Pays(pool, cost, required);
    }

    internal static string ResourceRequirement(AbilityCost? cost, Card card) => cost switch
    {
        null => string.Empty,
        AbilityCost.Spend { PrintedOnly: false } spend => spend.Resources,
        AbilityCost.Spend { PrintedOnly: true } => throw new RulesNotImplementedException(
            $"event '{card.FaceId}' combines its printed card cost with a printed-resource "
            + "arrow cost, whose allocation is not implemented"),
        AbilityCost.Sequence sequence => string.Concat(sequence.Costs
            .Select(step => step switch
            {
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

    /// <summary>What an action's cost looks like on a prompt, or null.</summary>
    /// <remarks>
    /// Resource generation travels in CostOption. Card-valued payments use a
    /// separate TargetRequest from AbilityCostSelection. A cost that names its
    /// own card needs neither choice representation.
    /// </remarks>
    internal static CostOption? Price(World world, Card card, int player, AbilityCost? cost)
    {
        if (cost is AbilityCost.Sequence sequence)
        {
            var prices = sequence.Costs
                .Select(step => Price(world, card, player, step))
                .Where(price => price is not null)
                .Cast<CostOption>()
                .ToList();
            if (prices.Count == 0)
            {
                return null;
            }
            if (prices.Count == 1)
            {
                return prices[0];
            }

            if (prices.Any(price => price.HasAlternative)
                || prices.Any(price => !long.TryParse(
                    price.Cost, System.Globalization.CultureInfo.InvariantCulture,
                    out _)))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has multiple resource costs whose combined price "
                    + "cannot be represented");
            }

            var components = prices.SelectMany(price => price.ResourceCosts).ToList();
            bool hasPrinted = components.Any(component => component.Printed);
            if (hasPrinted && components.Any(component => !component.Printed))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' mixes printed and ordinary simultaneous "
                    + "resource costs, whose allocation is not implemented");
            }

            long total = prices.Sum(price => long.Parse(
                price.Cost, System.Globalization.CultureInfo.InvariantCulture));
            return new CostOption(
                card.ObjectId,
                total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Rule: [string.Concat(prices.SelectMany(price => price.Rule ?? []))],
                Sources: prices
                    .SelectMany(price => price.Generators)
                    .GroupBy(source => source.Effect)
                    .Select(group => group.First())
                    .ToList(),
                Components: components);
        }

        if (cost is AbilityCost.SpendEnergy)
        {
            long maximum = CardPlay.Generators(
                    world, world.Facts, world.Seats[player])
                .Sum(source => source.Generates.LongCount(resource =>
                    resource is Resources.Energy or Resources.Wild));
            return new CostOption(
                card.ObjectId, "X", ["Y"],
                Sources: CardPlay.Generators(world, world.Facts, world.Seats[player]),
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
                Sources: PrintedGenerators(world, player),
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
            Sources: CardPlay.Generators(world, world.Facts, world.Seats[player]));
    }

    internal static CostOption? CombinedPrice(
        World world, Card card, int player, CompiledCardAbility ability)
    {
        var printed = EventPrice(world, card, player, ability.Effect);
        var arrow = Price(world, card, player, ability.Cost);
        if (printed is null || arrow is null)
        {
            return printed ?? arrow;
        }

        if (!long.TryParse(
                printed.Cost, System.Globalization.CultureInfo.InvariantCulture,
                out long printedAmount)
            || !long.TryParse(
                arrow.Cost, System.Globalization.CultureInfo.InvariantCulture,
                out long arrowAmount)
            || printed.HasAlternative || arrow.HasAlternative
            || printed.VariableRequests.Count > 0 || arrow.VariableRequests.Count > 0)
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

    internal static CostOption? EventPrice(
        World world, Card card, int player, AbilityEffect effect)
    {
        if (world.Facts.Kind(card.FaceId) != CardKind.Event)
        {
            return null;
        }

        long cost = CardPlay.CostOf(
            world, world.Facts, world.Seats[player], card).Amount;
        return new CostOption(
            Target: card.ObjectId,
            Cost: cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: Resources.Required(world, card, world.Facts) is { Length: > 0 } required
                ? [required]
                : null,
            Sources: EventGenerators(world, card, player, effect),
            DeclarationSensitive: PaidResourceQueries(effect).Any());
    }

    internal static List<ResourceSource> EventGenerators(
        World world, Card card, int player, AbilityEffect effect)
    {
        return CardPlay.Paying(world, world.Facts, world.Seats[player], card)
            .SelectMany(seat => CardPlay.Generators(world, world.Facts, seat, card))
            .Where(source => source.Effect != card.ObjectId)
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

    internal static List<ResourceSource> PrintedGenerators(World world, int player)
    {
        var hand = world.Seats[player].Hand.Cards
            .Select(card => new ResourceSource(
                card.ObjectId,
                Resources.GeneratedBy(card.FaceId, world.Facts)))
            .Where(source => source.Generates.Length > 0);
        return hand
            .Concat(world.Abilities.PrintedResourceAbilities(world, player))
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>Validates every selected cost before any simultaneous cost is paid.</summary>
    internal static void ValidatePayment(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        World world, Card source, int player)
        => ValidatePayment(cost, paying, chosen, values: null, world, source, player);

    internal static void ValidatePayment(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, World world, Card source, int player)
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
        if (spends.Count > 0)
        {
            if (paying.Distinct().Count() != paying.Count || paying.Intersect(chosen).Any())
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' names a generator more than once across its costs");
            }

            bool printed = spends.All(step => step.PrintedOnly);
            if (!printed && spends.Any(step => step.PrintedOnly))
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' mixes printed and ordinary simultaneous "
                    + "resource costs, whose allocation is not implemented");
            }
            var generators = printed
                ? PrintedGenerators(world, player)
                : CardPlay.Generators(
                    world, world.Facts, world.Seats[player]).ToList();
            var selected = paying.ToHashSet();
            if (paying.Any(id => generators.All(source => source.Effect != id)))
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' names a resource source that is not available");
            }

            string generated = string.Concat(generators
                .Where(source => selected.Contains(source.Effect))
                .Select(source => source.Generates));
            string required = string.Concat(spends.Select(step => step.Resources));
            bool pays = printed
                ? Resources.PaysPrinted(generated, required.Length, required)
                : Resources.Pays(generated, required.Length, required);
            if (!pays)
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' has simultaneous resource costs requiring "
                    + $"'{required}' and the payment generates '{generated}'");
            }
        }

        foreach (var step in steps.Where(
                     step => step is not AbilityCost.Spend))
        {
            if (step is AbilityCost.SpendEnergy)
            {
                long x = DefinedVariable(values, "X", source);
                var request = Price(world, source, player, step)!
                    .VariableRequests.Single(variable =>
                        string.Equals(variable.Name, "X", StringComparison.Ordinal));
                if (!request.Allows(x))
                {
                    throw new RulesNotImplementedException(
                        $"'{source.FaceId}' defines X as {x}, outside the offered "
                        + $"range {request.Min}..{request.Max}");
                }

                var generators = CardPlay.Generators(
                    world, world.Facts, world.Seats[player]).ToList();
                var selected = paying.ToHashSet();
                if (paying.Count == 0
                    || paying.Distinct().Count() != paying.Count
                    || paying.Any(id => generators.All(source => source.Effect != id)))
                {
                    throw new RulesNotImplementedException(
                        $"'{source.FaceId}' names an invalid generator for X");
                }

                string generated = string.Concat(generators
                    .Where(source => selected.Contains(source.Effect))
                    .Select(source => source.Generates));
                if (!Resources.Pays(
                        generated, x,
                        new string(Resources.Energy, checked((int)x))))
                {
                    throw new RulesNotImplementedException(
                        $"'{source.FaceId}' defines X as {x}, but the payment "
                        + $"generates '{generated}'");
                }
            }
            else if (step is AbilityCost.DiscardFromHand discard)
            {
                var hand = world.Seats[player].Hand;
                var (minimum, maximum) = AbilityCostSelection.Range(discard.Range, hand.Cards.Count);
                if (chosen.Count < minimum || chosen.Count > maximum
                    || chosen.Distinct().Count() != chosen.Count)
                {
                    string required = minimum == maximum
                        ? $"{minimum}"
                        : $"{minimum}..{maximum}";
                    throw new RulesNotImplementedException(
                        $"'{source.FaceId}' costs {required} card(s) from "
                        + $"hand and {chosen.Count} were chosen; "
                        + "rr:initiating-abilities.step.5 aborts without paying");
                }

                foreach (int id in chosen)
                {
                    if (world.Cards[id].Area != hand)
                    {
                        throw new RulesNotImplementedException(
                            $"card {id} is not in {world.Seats[player].Name}'s hand "
                            + "and cannot be discarded from it");
                    }
                }
            }
            else if (step is AbilityCost.ExhaustChosen exhaust)
            {
                var legal = AbilityCostSelection.Choices(world, player, exhaust.From)
                    .Where(card => card.Ready)
                    .Select(card => card.ObjectId)
                    .ToHashSet();
                var range = AbilityCostSelection.Range(exhaust.Range, legal.Count);
                if (chosen.Count < range.Min || chosen.Count > range.Max
                    || chosen.Distinct().Count() != chosen.Count
                    || chosen.Any(id => !legal.Contains(id)))
                {
                    throw new RulesNotImplementedException(
                        $"'{source.FaceId}' requires {range.Min}..{range.Max} legal "
                        + $"cards to exhaust and {chosen.Count} were supplied");
                }
            }
            else if (!Payable(world, source, player, step))
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' cannot pay its compiled cost");
            }
        }
    }

    internal static long DefinedVariable(
        IReadOnlyDictionary<string, long>? values, string name, Card source) =>
        values is not null && values.TryGetValue(name, out long value)
            ? value
            : throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires an explicit value for {name}");

}
