using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
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
    private static bool Payable(World world, Card card, int player, AbilityCost? cost) =>
        cost switch
        {
            null => true,
            AbilityCost.Sequence sequence => SequencePayable(world, card, player, sequence),
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
                && world.Abilities.CanTakeDamage(world, takingTarget, card)
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
    private static bool MandatoryCostIsAutomatic(AbilityCost cost) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.All(MandatoryCostIsAutomatic),
        AbilityCost.Discard or AbilityCost.Exhaust or AbilityCost.RemoveCounters => true,
        _ => false,
    };

    private static bool SequencePayable(World world, Card card, int player, AbilityCost.Sequence cost)
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
            .All(step => Payable(world, card, player, step));
    }

    private static bool CounterCostsPayable(
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

    private static IEnumerable<AbilityCost.RemoveCounters> CounterCostSteps(AbilityCost cost)
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

    private static Card? CostTarget(World world, Card source, int player, AbilityCostCard value) => value switch
    {
        AbilityCostCard.Source => source,
        AbilityCostCard.Identity => player >= 0 ? world.Seats[player].IdentityCard : null,
        _ => throw new InvalidOperationException("Unknown compiled cost card"),
    };

    private bool EventPayable(
        World world, Card card, int player, CardAbility ability)
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
            + ResourceRequirement(CompiledCost(ability), card);
        cost += required.Length
            - Resources.Required(world, card, world.Facts).Length;
        string pool = string.Concat(EventGenerators(world, card, player, ability.Effect)
            .SelectMany(source => source.Generates));
        return Resources.Pays(pool, cost, required);
    }

    private static string ResourceRequirement(AbilityCost? cost, Card card) => cost switch
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
    /// Only a resource cost reaches the wire, because only a resource cost is a
    /// <i>choice</i>. Exhausting the card the ability is on has one way to be
    /// paid, so there is nothing to ask and nothing to carry.
    /// </remarks>
    private static CostOption? Price(World world, Card card, int player, AbilityCost? cost)
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

    private CostOption? CombinedPrice(
        World world, Card card, int player, CardAbility ability)
    {
        var printed = EventPrice(world, card, player, ability.Effect);
        var arrow = Price(world, card, player, CompiledCost(ability));
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

    private static CostOption? EventPrice(
        World world, Card card, int player, AbilityNode effect)
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
            DeclarationSensitive: PaidResourceQueries(effect.Argument).Any()
                || effect.Kind == "paidWithResource");
    }

    private static List<ResourceSource> EventGenerators(
        World world, Card card, int player, AbilityNode effect)
    {
        return CardPlay.Paying(world, world.Facts, world.Seats[player], card)
            .SelectMany(seat => CardPlay.Generators(world, world.Facts, seat, card))
            .Where(source => source.Effect != card.ObjectId)
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
    }

    private static List<ResourceSource> PrintedGenerators(World world, int player)
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

    private static void PayEvent(
        Card card, IReadOnlyList<int> paying, Cast cast, AbilityNode effect,
        IReadOnlyList<ResourceAllocation>? allocations = null,
        AbilityCost? additionalCost = null)
    {
        if (cast.World.Facts.Kind(card.FaceId) != CardKind.Event)
        {
            return;
        }

        if (!Resources.HasPlayableCost(card.FaceId, cast.World.Facts))
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' has no payable printed cost");
        }

        var adjusted = CardPlay.CostOf(
            cast.World, cast.World.Facts, cast.World.Seats[cast.Player], card);
        var payingSeats = CardPlay.Paying(
            cast.World, cast.World.Facts, cast.World.Seats[cast.Player], card);
        var generators = payingSeats
            .SelectMany(seat => CardPlay.Generators(
                cast.World, cast.World.Facts, seat, card))
            .Where(source => source.Effect != card.ObjectId)
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
        var resourcePayers = payingSeats
            .SelectMany(seat => cast.World.Abilities.ResourceAbilities(
                    cast.World, seat.Index)
                .Select(source => (source.Effect, seat.Index)))
            .GroupBy(entry => entry.Effect)
            .ToDictionary(group => group.Key, group => group.First().Index);
        var selected = paying.ToHashSet();
        if (selected.Count != paying.Count
            || paying.Any(id => generators.All(source => source.Effect != id)))
        {
            throw new RulesNotImplementedException(
                $"the payment for event {card.ObjectId} names a source that is not available");
        }

        string generated = string.Concat(generators
            .Where(source => selected.Contains(source.Effect))
            .Select(source => source.Generates));
        string printedRequired = Resources.Required(
            cast.World, card, cast.World.Facts);
        string additionalRequired = ResourceRequirement(additionalCost, card);
        string required = printedRequired + additionalRequired;
        long total = checked(adjusted.Amount + additionalRequired.Length);
        var components = new List<ResourceCost>
        {
            new(
                adjusted.Amount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                printedRequired.Length > 0 ? [printedRequired] : null),
        };
        if (additionalRequired.Length > 0)
        {
            components.Add(new ResourceCost(
                additionalRequired.Length.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                [additionalRequired]));
        }

        if (!Resources.Pays(generated, total, required))
        {
            throw new RulesNotImplementedException(
                $"the cost is {total}"
                + (required.Length > 0 ? $" requiring '{required}'" : string.Empty)
                + $" and the payment generates '{generated}'; "
                + "rr:initiating-abilities.step.5 aborts without paying");
        }

        var assigned = allocations ?? [];
        if (components.Count > 1 && assigned.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} has simultaneous printed and arrow resource "
                + "costs whose icon allocation was not supplied");
        }
        if (assigned.Count == 0 && HasAmbiguousPaidResourceAllocation(
                effect, generated, total, required))
        {
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} overpays with unlike resource types, whose paid "
                + "allocation is not represented");
        }

        // Validate the player's complete icon allocation before step 1 moves
        // the event. A malformed allocation is an invalid payment answer and
        // `rr:initiating-abilities.step.5` must reject it without changing the
        // board.
        bool declarationSensitive = PaidResourceQueries(effect.Argument).Any()
            || effect.Kind == "paidWithResource";
        string paid = assigned.Count > 0
            ? AllocatedResources(generators, paying, assigned, components, card)
            : declarationSensitive
                ? DeclaredPaidResources(generated, total, required)
                : Resources.Paid(generated, total, required);

        // `rr:initiating-abilities.step.1` and `rr:event`: the event leaves the
        // hand faceup and out of play before costs are paid, and remains there
        // while a choice suspends its resolution. RevealingArea already has
        // exactly those state semantics; the player's play area distinguishes
        // this event from encounter cards being revealed elsewhere.
        var from = card.Area;
        var resolving = cast.World.AreaOf(
            DeckType.RevealingArea, PlayArea.Of(cast.Player), cardOwner: card.Owner);
        World.MoveToTop(card, resolving);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(resolving),
            [new Landing(card.ObjectId, resolving.Cards.Count - 1)])
        {
            Trigger = CardPlay.Verb,
            Verb = CardPlay.Verb,
        });

        cast.PaidWith(paid);
        foreach (char resource in paid.Distinct())
        {
            cast.World.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: "paid:" + resource,
                Card: card.ObjectId,
                Affects: card.ObjectId,
                Lasts: new Duration(Uses: 1)));
        }

        CardPlay.Spend(
            cast.World, cast.World.Facts, [.. payingSeats.Select(seat => seat.Hand)], paying,
            total,
            required, card.ObjectId,
            cast.Player, cast.Events, payingFor: card,
            resourcePayers: resourcePayers);
        CardPlay.UseCostModifiers(cast.World, adjusted);

        // `rr:initiating-abilities.step.6`: after its costs are paid, the
        // event is played and its effect resolves. The action's persistent
        // occurrence owns the response window after that effect, so add the
        // condition here rather than creating an earlier separate window.
        if (cast.Occurrence.Is(Steps.TurnAction))
        {
            cast.Occurrence.Also(Steps.CardPlayed);
        }
    }

    private static string AllocatedResources(
        IReadOnlyList<ResourceSource> generators,
        IReadOnlyList<int> paying,
        IReadOnlyList<ResourceAllocation> allocations,
        List<ResourceCost> components,
        Card card)
    {
        var selected = paying.ToHashSet();
        var remaining = generators
            .Where(source => selected.Contains(source.Effect))
            .ToDictionary(
                source => source.Effect,
                source => source.Generates.ToList());
        var paid = Enumerable.Range(0, components.Count)
            .Select(_ => new System.Text.StringBuilder())
            .ToList();

        foreach (var allocation in allocations)
        {
            if (!remaining.TryGetValue(allocation.Source, out var available)
                || allocation.Cost < 0 || allocation.Cost >= components.Count
                || allocation.PaidAs.Length == 0)
            {
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} carries an invalid resource allocation");
            }

            foreach (char declared in allocation.PaidAs)
            {
                if (!Resources.Types.Contains(declared))
                {
                    throw new RulesNotImplementedException(
                        $"event {card.ObjectId} declares unknown resource '{declared}'");
                }

                int icon = available.IndexOf(declared);
                if (icon < 0
                    && !components[allocation.Cost].Printed
                    && declared != Resources.Wild)
                {
                    icon = available.IndexOf(Resources.Wild);
                }
                if (icon < 0)
                {
                    throw new RulesNotImplementedException(
                        $"event {card.ObjectId} allocates '{declared}' from generator "
                        + $"{allocation.Source}, which cannot produce it");
                }

                available.RemoveAt(icon);
                paid[allocation.Cost].Append(declared);
            }
        }

        for (int index = 0; index < components.Count; index++)
        {
            if (!long.TryParse(
                    components[index].Cost,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out long amount))
            {
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} has a non-numeric allocation component");
            }
            string assigned = paid[index].ToString();
            string required = string.Concat(components[index].Rule ?? []);
            if (assigned.Length != amount
                || !Resources.PaysDeclared(assigned, amount, required))
            {
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} assigns '{assigned}' to cost {index}, "
                    + $"which costs {amount}"
                    + (required.Length > 0 ? $" requiring '{required}'" : string.Empty));
            }
        }

        return string.Concat(paid.Select(component => component.ToString()));
    }

    private static bool HasAmbiguousPaidResourceAllocation(
        AbilityNode effect, string generated, long cost, string required)
    {
        var queried = PaidResourceQueries(effect.Argument)
            .Concat(effect.Kind == "paidWithResource"
                ? [Word(effect.Argument)[0]]
                : [])
            .Distinct()
            .ToList();
        if (queried.Count == 0)
        {
            return false;
        }

        var outcomes = queried.ToDictionary(
            resource => resource,
            _ => (Paid: false, NotPaid: false));
        var selected = new char[(int)cost];
        return Search(start: 0, chosen: 0);

        bool Search(int start, int chosen)
        {
            if (chosen == selected.Length)
            {
                string payment = new(selected);
                var declared = payment.ToCharArray();
                return DeclareWild(index: 0);

                bool DeclareWild(int index)
                {
                    while (index < declared.Length && declared[index] != Resources.Wild)
                    {
                        index++;
                    }
                    if (index < declared.Length)
                    {
                        foreach (char declaration in Resources.Types)
                        {
                            declared[index] = declaration;
                            if (DeclareWild(index + 1))
                            {
                                return true;
                            }
                        }
                        declared[index] = Resources.Wild;
                        return false;
                    }

                    var pool = declared.ToList();
                    foreach (char requiredType in required)
                    {
                        int found = pool.IndexOf(requiredType);
                        if (found < 0)
                        {
                            return false;
                        }
                        pool.RemoveAt(found);
                    }

                    foreach (char resource in queried)
                    {
                        bool paid = declared.Contains(resource);
                        var seen = outcomes[resource];
                        outcomes[resource] = paid
                            ? (true, seen.NotPaid)
                            : (seen.Paid, true);
                        if (outcomes[resource] is (true, true))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            int left = selected.Length - chosen;
            for (int index = start; index <= generated.Length - left; index++)
            {
                selected[chosen] = generated[index];
                if (Search(index + 1, chosen + 1))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private static IEnumerable<char> PaidResourceQueries(AbilityValue value)
    {
        if (value is AbilityValue.List list)
        {
            foreach (char resource in list.Values.SelectMany(PaidResourceQueries))
            {
                yield return resource;
            }
            yield break;
        }

        if (value is not AbilityValue.Map map)
        {
            yield break;
        }

        foreach (var (kind, argument) in map.Entries)
        {
            if (kind == "paidWithResource")
            {
                yield return Word(argument)[0];
            }
            foreach (char resource in PaidResourceQueries(argument))
            {
                yield return resource;
            }
        }
    }

    private static string DeclaredPaidResources(
        string generated, long cost, string required)
    {
        var selected = new char[(int)cost];
        string? declaredPayment = null;
        Search(start: 0, chosen: 0);
        return declaredPayment
            ?? throw new RulesNotImplementedException(
                "the generated resources have no legal declared payment");

        bool Search(int start, int chosen)
        {
            if (chosen == selected.Length)
            {
                var declared = selected.ToArray();
                return DeclareWild(index: 0);

                bool DeclareWild(int index)
                {
                    while (index < declared.Length && declared[index] != Resources.Wild)
                    {
                        index++;
                    }
                    if (index < declared.Length)
                    {
                        foreach (char declaration in Resources.Types)
                        {
                            declared[index] = declaration;
                            if (DeclareWild(index + 1))
                            {
                                return true;
                            }
                        }
                        declared[index] = Resources.Wild;
                        return false;
                    }

                    var pool = declared.ToList();
                    foreach (char requiredType in required)
                    {
                        int found = pool.IndexOf(requiredType);
                        if (found < 0)
                        {
                            return false;
                        }
                        pool.RemoveAt(found);
                    }

                    declaredPayment = new string(declared);
                    return true;
                }
            }

            int left = selected.Length - chosen;
            for (int index = start; index <= generated.Length - left; index++)
            {
                selected[chosen] = generated[index];
                if (Search(index + 1, chosen + 1))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private static void DiscardEvent(Card card, Cast cast)
    {
        bool playedInWindow = !cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner)
            && !cast.Occurrence.Is(Steps.TurnAction);
        if (!cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner))
        {
            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
            foreach (var payment in cast.World.Effects.Active().Where(effect =>
                effect.Card == card.ObjectId
                && effect.Kind.StartsWith("paid:", StringComparison.Ordinal)).ToList())
            {
                cast.World.Effects.Use(payment);
            }

            if (playedInWindow)
            {
                cast.World.Agenda.NowEventPlayed(
                    cast.World.Agenda.Current?.Round ?? 0,
                    card.ObjectId,
                    cast.Player);
            }
        }
    }

    /// <summary>Pays an ability's cost — <c>rr:initiating-abilities.step.5</c>.</summary>
    private static void PayNonResourceCosts(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null or AbilityCost.Spend or AbilityCost.SpendEnergy)
        {
            return;
        }
        if (cost is AbilityCost.Sequence sequence)
        {
            foreach (var step in sequence.Costs)
            {
                PayNonResourceCosts(step, paying, chosen, values, cast);
            }
            return;
        }

        Pay(cost, paying, chosen, values, cast);
    }

    /// <summary>Pays an ability's cost — <c>rr:initiating-abilities.step.5</c>.</summary>
    private static void Pay(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, Cast cast)
        => Pay(cost, paying, chosen, values: null, cast);

    private static void Pay(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null)
        {
            return;
        }

        if (cost is AbilityCost.Sequence sequence)
        {
            var steps = sequence.Costs;
            // A take-damage cost is not paid unless all of the damage is
            // taken. Resolve that uncertain component before any payment that
            // cannot fail after validation, so a prevention never leaves an
            // otherwise simultaneous card or resource cost paid by itself.
            foreach (var step in steps.Where(step => step is AbilityCost.Damage { MustTakeAll: true }))
            {
                Pay(step, paying, chosen, values, cast);
            }
            var spends = steps.OfType<AbilityCost.Spend>().ToList();
            if (spends.Count > 0)
            {
                bool printed = spends.All(step => step.PrintedOnly);
                if (!printed && spends.Any(step => step.PrintedOnly))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' mixes printed and ordinary simultaneous "
                        + "resource costs, whose allocation is not implemented");
                }
                string required = string.Concat(spends.Select(step => step.Resources));
                SpendAbilityResources(required, paying, cast, printed);
            }

            foreach (var step in steps.Where(
                         step => step is not (AbilityCost.Spend or AbilityCost.Damage { MustTakeAll: true })))
            {
                Pay(step, paying, chosen, values, cast);
            }
            return;
        }

        if (cost is AbilityCost.DiscardFromHand discard)
        {
            DiscardToPay(discard, chosen, cast);
            return;
        }

        if (cost is AbilityCost.ExhaustChosen)
        {
            foreach (int id in chosen)
            {
                var target = cast.World.Cards[id];
                target.Exhaust();
                cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
                {
                    Trigger = cast.Trigger,
                    Verb = "Exhaust",
                });
            }
            return;
        }

        if (cost is AbilityCost.Spend spend)
        {
            SpendAbilityResources(spend.Resources, paying, cast, spend.PrintedOnly);
            return;
        }

        if (cost is AbilityCost.SpendEnergy)
        {
            if (paying.Count == 0 || paying.Distinct().Count() != paying.Count)
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' requires one or more distinct generators for X");
            }

            var selected = paying.ToHashSet();
            string generated = string.Concat(CardPlay.Generators(
                    cast.World, cast.World.Facts, cast.World.Seats[cast.Player])
                .Where(source => selected.Contains(source.Effect))
                .Select(source => source.Generates));
            long x = DefinedVariable(values, "X", cast.Source);
            CardPlay.Spend(
                cast.World, cast.World.Facts, [cast.World.Seats[cast.Player].Hand], paying,
                x, new string(Resources.Energy, checked((int)x)), itself: -1,
                cast.Player, cast.Events);
            cast.Results["energy"] = x;
            return;
        }

        if (cost is AbilityCost.Damage { MustTakeAll: true } damage)
        {
            var target = CostTarget(
                    cast.World, cast.Source, cast.Player, damage.Card)
                ?? throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' cannot find its take-damage cost target");
            long amount = damage.Amount;
            long before = target.Damage;
            var outcome = Damage.DealOutcome(
                cast.World, cast.World.Facts, cast.Source, target, amount,
                cast.Trigger, CardPlay.Verb, cast.Events);
            long taken = target.Damage - before;
            if (taken != amount)
            {
                // `rr:cost.12`: "If any of the damage is prevented, then the
                // cost has not been paid." The prevention itself has happened,
                // but neither another cost nor the post-arrow effect has.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' requires {amount} damage to be taken as a "
                    + $"cost, but only {taken} was taken; rr:cost.12 leaves it unpaid");
            }
            if (outcome == Damage.Outcome.Suspended)
            {
                cast.Results["costProcedurePending"] = 1;
                cast.Suspend();
            }
            return;
        }

        PayPrimitiveCost(cost, cast);
    }

    private static void SpendAbilityResources(
        string required, IReadOnlyList<int> paying, Cast cast, bool printed = false) =>
        CardPlay.Spend(
            cast.World,
            cast.World.Facts,
            [cast.World.Seats[cast.Player].Hand],
            paying,
            required.Length,
            required,
            itself: -1,
            cast.Player,
            cast.Events);

    /// <summary>Validates every selected cost before any simultaneous cost is paid.</summary>
    private static void ValidatePayment(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, Cast cast)
        => ValidatePayment(cost, paying, chosen, values: null, cast);

    private static void ValidatePayment(
        AbilityCost? cost, IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values, Cast cast)
    {
        if (cost is null)
        {
            return;
        }

        IReadOnlyList<AbilityCost> steps = cost is AbilityCost.Sequence sequence ? sequence.Costs : [cost];
        if (cast.World.Facts.Kind(cast.Source.FaceId) == CardKind.Event
            && steps.Any(step => step is AbilityCost.Damage { MustTakeAll: true }))
        {
            // Playing an event pays its printed resource price through
            // PayEvent. Rolling that payment back after damage prevention is a
            // transaction the engine does not yet represent, so refuse it
            // before the event or a generator moves.
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' is an event with a take-damage cost; "
                + "atomic printed and damage payment is not implemented");
        }
        var spends = steps.OfType<AbilityCost.Spend>().ToList();
        if (spends.Count > 0)
        {
            if (paying.Distinct().Count() != paying.Count || paying.Intersect(chosen).Any())
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' names a generator more than once across its costs");
            }

            bool printed = spends.All(step => step.PrintedOnly);
            if (!printed && spends.Any(step => step.PrintedOnly))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' mixes printed and ordinary simultaneous "
                    + "resource costs, whose allocation is not implemented");
            }
            var generators = printed
                ? PrintedGenerators(cast.World, cast.Player)
                : CardPlay.Generators(
                    cast.World, cast.World.Facts, cast.World.Seats[cast.Player]).ToList();
            var selected = paying.ToHashSet();
            if (paying.Any(id => generators.All(source => source.Effect != id)))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' names a resource source that is not available");
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
                    $"'{cast.Source.FaceId}' has simultaneous resource costs requiring "
                    + $"'{required}' and the payment generates '{generated}'");
            }
        }

        foreach (var step in steps.Where(
                     step => step is not AbilityCost.Spend))
        {
            if (step is AbilityCost.SpendEnergy)
            {
                long x = DefinedVariable(values, "X", cast.Source);
                var request = Price(cast.World, cast.Source, cast.Player, step)!
                    .VariableRequests.Single(variable =>
                        string.Equals(variable.Name, "X", StringComparison.Ordinal));
                if (!request.Allows(x))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' defines X as {x}, outside the offered "
                        + $"range {request.Min}..{request.Max}");
                }

                var generators = CardPlay.Generators(
                    cast.World, cast.World.Facts, cast.World.Seats[cast.Player]).ToList();
                var selected = paying.ToHashSet();
                if (paying.Count == 0
                    || paying.Distinct().Count() != paying.Count
                    || paying.Any(id => generators.All(source => source.Effect != id)))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' names an invalid generator for X");
                }

                string generated = string.Concat(generators
                    .Where(source => selected.Contains(source.Effect))
                    .Select(source => source.Generates));
                if (!Resources.Pays(
                        generated, x,
                        new string(Resources.Energy, checked((int)x))))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' defines X as {x}, but the payment "
                        + $"generates '{generated}'");
                }
            }
            else if (step is AbilityCost.DiscardFromHand discard)
            {
                var hand = cast.World.Seats[cast.Player].Hand;
                var (minimum, maximum) = AbilityCostSelection.Range(discard.Range, hand.Cards.Count);
                if (chosen.Count < minimum || chosen.Count > maximum
                    || chosen.Distinct().Count() != chosen.Count)
                {
                    string required = minimum == maximum
                        ? $"{minimum}"
                        : $"{minimum}..{maximum}";
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' costs {required} card(s) from "
                        + $"hand and {chosen.Count} were chosen; "
                        + "rr:initiating-abilities.step.5 aborts without paying");
                }

                foreach (int id in chosen)
                {
                    if (cast.World.Cards[id].Area != hand)
                    {
                        throw new RulesNotImplementedException(
                            $"card {id} is not in {cast.World.Seats[cast.Player].Name}'s hand "
                            + "and cannot be discarded from it");
                    }
                }
            }
            else if (step is AbilityCost.ExhaustChosen exhaust)
            {
                var legal = AbilityCostSelection.Choices(cast.World, cast.Player, exhaust.From)
                    .Where(card => card.Ready)
                    .Select(card => card.ObjectId)
                    .ToHashSet();
                var range = AbilityCostSelection.Range(exhaust.Range, legal.Count);
                if (chosen.Count < range.Min || chosen.Count > range.Max
                    || chosen.Distinct().Count() != chosen.Count
                    || chosen.Any(id => !legal.Contains(id)))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' requires {range.Min}..{range.Max} legal "
                        + $"cards to exhaust and {chosen.Count} were supplied");
                }
            }
            else if (!Payable(cast.World, cast.Source, cast.Player, step))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' cannot pay its compiled cost");
            }
        }
    }

    private static long DefinedVariable(
        IReadOnlyDictionary<string, long>? values, string name, Card source) =>
        values is not null && values.TryGetValue(name, out long value)
            ? value
            : throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires an explicit value for {name}");

    /// <summary>
    /// "Discard a card from your hand" — a cost whose payment is a card and not
    /// a number of resources.
    /// </summary>
    /// <remarks>
    /// Refused rather than corrected when the answer does not match the
    /// request. <c>rr:initiating-abilities.step.5</c> aborts "without paying
    /// any costs" if the cost cannot be paid, and an engine that picked a card
    /// for the player would be making a decision the player was asked to make.
    /// </remarks>
    private static void DiscardToPay(AbilityCost.DiscardFromHand cost, IReadOnlyList<int> chosen, Cast cast)
    {
        var hand = cast.World.Seats[cast.Player].Hand;
        var (minimum, maximum) = AbilityCostSelection.Range(cost.Range, hand.Cards.Count);

        if (chosen.Count < minimum || chosen.Count > maximum)
        {
            string required = minimum == maximum
                ? $"{minimum}"
                : $"{minimum}..{maximum}";
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' costs {required} card(s) from hand and "
                + $"{chosen.Count} "
                + "were chosen; rr:initiating-abilities.step.5 aborts without paying");
        }

        foreach (int id in chosen)
        {
            var card = cast.World.Cards[id];
            if (card.Area != hand)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is not in {cast.World.Seats[cast.Player].Name}'s hand "
                    + "and cannot be discarded from it");
            }

            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
        }
    }

}
