using System.Collections.Immutable;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>A validated event resource payment, ready for immediate commitment.</summary>
/// <remarks>Owns selected payment facts, not ability resolution or continuation state.</remarks>
internal sealed class AbilityEventPayment
{
    private readonly World world;
    private readonly Card card;
    private readonly int player;
    private readonly ImmutableArray<int> paying;
    private readonly ImmutableArray<Area> hands;
    private readonly ImmutableDictionary<int, int> resourcePayers;
    private readonly AdjustedCardCost adjusted;
    private readonly long total;
    private readonly string required;
    private readonly string paid;

    private AbilityEventPayment(
        World world, Card card, int player, ImmutableArray<int> paying,
        ImmutableArray<Area> hands, ImmutableDictionary<int, int> resourcePayers,
        AdjustedCardCost adjusted, long total, string required, string paid)
    {
        this.world = world;
        this.card = card;
        this.player = player;
        this.paying = paying;
        this.hands = hands;
        this.resourcePayers = resourcePayers;
        this.adjusted = adjusted;
        this.total = total;
        this.required = required;
        this.paid = paid;
    }

    internal static AbilityEventPayment? Prepare(
        World world, Card card, int player, IReadOnlyList<int> paying, AbilityEffect effect,
        IReadOnlyList<ResourceAllocation>? allocations = null,
        AbilityCost? additionalCost = null)
    {
        if (world.Facts.Kind(card.FaceId) != CardKind.Event)
        {
            return null;
        }

        if (!Resources.HasPlayableCost(card.FaceId, world.Facts))
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' has no payable printed cost");
        }

        var adjusted = CardPlay.CostOf(
            world, world.Facts, world.Seats[player], card);
        var payingSeats = CardPlay.Paying(
            world, world.Facts, world.Seats[player], card);
        var generators = payingSeats
            .SelectMany(seat => CardPlay.Generators(
                world, world.Facts, seat, card))
            .Where(source => source.Effect != card.ObjectId)
            .GroupBy(source => source.Effect)
            .Select(group => group.First())
            .ToList();
        var resourcePayers = payingSeats
            .SelectMany(seat => world.Abilities.ResourceAbilities(
                    world, seat.Index)
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
            world, card, world.Facts);
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
        bool declarationSensitive = PaidResourceQueries(effect).Any();
        string paid = assigned.Count > 0
            ? AllocatedResources(generators, paying, assigned, components, card)
            : declarationSensitive
                ? DeclaredPaidResources(generated, total, required)
                : Resources.Paid(generated, total, required);

        return new AbilityEventPayment(world, card, player, [.. paying],
            [.. payingSeats.Select(seat => seat.Hand)],
            resourcePayers.ToImmutableDictionary(),
            adjusted with { Modifiers = adjusted.Modifiers.ToImmutableArray() },
            total, required, paid);
    }

    /// <summary>Commit the validated payment at the initiating boundary.</summary>
    internal string Commit(Occurrence occurrence, List<GameEvent> events)
    {
        // `rr:initiating-abilities.step.1` and `rr:event`: the event leaves the
        // hand faceup and out of play before costs are paid, and remains there
        // while a choice suspends its resolution. RevealingArea already has
        // exactly those state semantics; the player's play area distinguishes
        // this event from encounter cards being revealed elsewhere.
        var from = card.Area;
        var resolving = world.AreaOf(
            DeckType.RevealingArea, PlayArea.Of(player), cardOwner: card.Owner);
        World.MoveToTop(card, resolving);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(resolving),
            [new Landing(card.ObjectId, resolving.Cards.Count - 1)])
        {
            Trigger = CardPlay.Verb,
            Verb = CardPlay.Verb,
        });

        foreach (char resource in paid.Distinct())
        {
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: "paid:" + resource,
                Card: card.ObjectId,
                Affects: card.ObjectId,
                Lasts: new Duration(Uses: 1)));
        }

        CardPlay.Spend(
            world, world.Facts, hands, paying,
            total,
            required, card.ObjectId,
            player, events, payingFor: card,
            resourcePayers: resourcePayers);
        CardPlay.UseCostModifiers(world, adjusted);

        // `rr:initiating-abilities.step.6`: after its costs are paid, the
        // event is played and its effect resolves. The action's persistent
        // occurrence owns the response window after that effect, so add the
        // condition here rather than creating an earlier separate window.
        if (occurrence.Is(Steps.TurnAction))
        {
            occurrence.Also(Steps.CardPlayed);
        }
        return paid;
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
        AbilityEffect effect, string generated, long cost, string required)
    {
        var queried = PaidResourceQueries(effect)
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

}
