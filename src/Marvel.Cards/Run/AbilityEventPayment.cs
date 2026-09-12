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
    private readonly IResourceCardAbilities resourceAbilities;
    private readonly ImmutableArray<int> paying;
    private readonly ImmutableArray<Area> hands;
    private readonly ImmutableDictionary<int, int> resourcePayers;
    private readonly AdjustedCardCost adjusted;
    private readonly long total;
    private readonly string required;
    private readonly string paid;

    private AbilityEventPayment(
        World world, Card card, int player, IResourceCardAbilities resourceAbilities,
        ImmutableArray<int> paying,
        ImmutableArray<Area> hands, ImmutableDictionary<int, int> resourcePayers,
        AdjustedCardCost adjusted, long total, string required, string paid)
    {
        this.world = world;
        this.card = card;
        this.player = player;
        this.resourceAbilities = resourceAbilities;
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
        IResourceCardAbilities resourceAbilities,
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

        var adjusted = CardPayment.CostOf(
            world, world.Facts, world.Seats[player], card);
        var sources = EventPaymentSources.Find(world, card, player, resourceAbilities);
        ValidateSelectedSources(card, paying, sources.Generators);

        string generated = GeneratedResources(sources.Generators, paying);
        var requirements = EventPaymentRequirements.Create(
            world, card, adjusted, additionalCost);
        ValidateGeneratedPayment(generated, requirements);

        var assigned = allocations ?? [];
        string paid = ResolvePaidResources(
            card, effect, sources.Generators, paying, assigned, generated, requirements);

        return new AbilityEventPayment(world, card, player, resourceAbilities, [.. paying],
            [.. sources.PayingSeats.Select(seat => seat.Hand)],
            sources.ResourcePayers.ToImmutableDictionary(),
            adjusted with { Modifiers = adjusted.Modifiers.ToImmutableArray() },
            requirements.Total, requirements.Required, paid);
    }

    private static string GeneratedResources(
        IReadOnlyList<ResourceSource> generators, IReadOnlyList<int> paying)
    {
        var selected = paying.ToHashSet();
        return string.Concat(generators
            .Where(source => selected.Contains(source.Effect))
            .Select(source => source.Generates));
    }

    private static void ValidateSelectedSources(
        Card card, IReadOnlyList<int> paying, IReadOnlyList<ResourceSource> generators)
    {
        var selected = paying.ToHashSet();
        if (selected.Count != paying.Count
            || paying.Any(id => generators.All(source => source.Effect != id)))
            throw new RulesNotImplementedException(
                $"the payment for event {card.ObjectId} names a source that is not available");
    }

    private static void ValidateGeneratedPayment(
        string generated, EventPaymentRequirements requirements)
    {
        if (Resources.Pays(generated, requirements.Total, requirements.Required)) return;
        throw new RulesNotImplementedException(
            $"the cost is {requirements.Total}"
            + (requirements.Required.Length > 0
                ? $" requiring '{requirements.Required}'" : string.Empty)
            + $" and the payment generates '{generated}'; "
            + "rr:initiating-abilities.step.5 aborts without paying");
    }

    private static string ResolvePaidResources(
        Card card, AbilityEffect effect, IReadOnlyList<ResourceSource> generators,
        IReadOnlyList<int> paying, IReadOnlyList<ResourceAllocation> assigned,
        string generated, EventPaymentRequirements requirements)
    {
        if (requirements.Components.Count > 1 && assigned.Count == 0)
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} has simultaneous printed and arrow resource costs whose icon allocation was not supplied");
        if (assigned.Count == 0 && HasAmbiguousPaidResourceAllocation(
                effect, generated, requirements.Total, requirements.Required))
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} overpays with unlike resource types, whose paid allocation is not represented");
        if (assigned.Count > 0)
            return AllocatedResources(
                generators, paying, assigned, requirements.Components, card);
        return PaidResourceQueries(effect).Any()
            ? DeclaredPaidResources(generated, requirements.Total, requirements.Required)
            : Resources.Paid(generated, requirements.Total, requirements.Required);
    }

    private sealed record EventPaymentSources(
        IReadOnlyList<Seat> PayingSeats, IReadOnlyList<ResourceSource> Generators,
        Dictionary<int, int> ResourcePayers)
    {
        internal static EventPaymentSources Find(
            World world, Card card, int player, IResourceCardAbilities abilities)
        {
            var seats = CardPayment.Paying(
                world, world.Facts, world.Seats[player], card).ToList();
            var generators = seats.SelectMany(seat => CardPayment.Generators(
                    world, world.Facts, seat, abilities, card))
                .Where(source => source.Effect != card.ObjectId)
                .GroupBy(source => source.Effect).Select(group => group.First()).ToList();
            var payers = seats.SelectMany(seat => abilities.ResourceAbilities(world, seat.Index)
                    .Select(source => (source.Effect, seat.Index)))
                .GroupBy(entry => entry.Effect)
                .ToDictionary(group => group.Key, group => group.First().Index);
            return new EventPaymentSources(seats, generators, payers);
        }
    }

    private sealed record EventPaymentRequirements(
        long Total, string Required, List<ResourceCost> Components)
    {
        internal static EventPaymentRequirements Create(
            World world, Card card, AdjustedCardCost adjusted, AbilityCost? additionalCost)
        {
            string printed = Resources.Required(world, card, world.Facts);
            string additional = ResourceRequirement(additionalCost, card);
            var components = new List<ResourceCost>
            {
                new(adjusted.Amount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                    printed.Length > 0 ? [printed] : null),
            };
            if (additional.Length > 0)
                components.Add(new ResourceCost(
                    additional.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture), [additional]));
            return new EventPaymentRequirements(
                checked(adjusted.Amount + additional.Length), printed + additional, components);
        }
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
            world, world.Facts, resourceAbilities, hands, paying,
            total,
            required, card.ObjectId,
            player, events, payingFor: card,
            resourcePayers: resourcePayers);
        CardPayment.UseCostModifiers(world, adjusted);

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
        Card card) => new ResourceAllocationLedger(
            generators, paying, components, card).Allocate(allocations);

    private sealed class ResourceAllocationLedger
    {
        private readonly List<ResourceCost> components;
        private readonly Card card;
        private readonly Dictionary<int, List<char>> remaining;
        private readonly List<System.Text.StringBuilder> paid;

        internal ResourceAllocationLedger(
            IReadOnlyList<ResourceSource> generators, IReadOnlyList<int> paying,
            List<ResourceCost> components, Card card)
        {
            this.components = components;
            this.card = card;
            var selected = paying.ToHashSet();
            remaining = generators.Where(source => selected.Contains(source.Effect))
            .ToDictionary(
                source => source.Effect,
                source => source.Generates.ToList());
            paid = Enumerable.Range(0, components.Count)
                .Select(_ => new System.Text.StringBuilder()).ToList();
        }

        internal string Allocate(IReadOnlyList<ResourceAllocation> allocations)
        {
            foreach (var allocation in allocations) Allocate(allocation);
            ValidateComponents();
            return string.Concat(paid.Select(component => component.ToString()));
        }

        private void Allocate(ResourceAllocation allocation)
        {
            if (!remaining.TryGetValue(allocation.Source, out var available)
                || allocation.Cost < 0 || allocation.Cost >= components.Count
                || allocation.PaidAs.Length == 0)
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} carries an invalid resource allocation");
            foreach (char declared in allocation.PaidAs)
                Consume(allocation, available, declared);
        }

        private void Consume(ResourceAllocation allocation, List<char> available, char declared)
        {
            if (!Resources.Types.Contains(declared))
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} declares unknown resource '{declared}'");
            int icon = available.IndexOf(declared);
            if (icon < 0 && !components[allocation.Cost].Printed
                && declared != Resources.Wild)
                icon = available.IndexOf(Resources.Wild);
            if (icon < 0)
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} allocates '{declared}' from generator {allocation.Source}, which cannot produce it");
            available.RemoveAt(icon);
            paid[allocation.Cost].Append(declared);
        }

        private void ValidateComponents()
        {
            for (int index = 0; index < components.Count; index++)
            {
                if (!long.TryParse(components[index].Cost,
                        System.Globalization.CultureInfo.InvariantCulture, out long amount))
                    throw new RulesNotImplementedException(
                        $"event {card.ObjectId} has a non-numeric allocation component");
                string assigned = paid[index].ToString();
                string required = string.Concat(components[index].Rule ?? []);
                if (assigned.Length != amount
                    || !Resources.PaysDeclared(assigned, amount, required))
                    throw new RulesNotImplementedException(
                        $"event {card.ObjectId} assigns '{assigned}' to cost {index}, which costs {amount}"
                        + (required.Length > 0 ? $" requiring '{required}'" : string.Empty));
            }
        }
    }

    private static bool HasAmbiguousPaidResourceAllocation(
        AbilityEffect effect, string generated, long cost, string required) =>
        new PaidResourceAmbiguitySearch(
            PaidResourceQueries(effect).Distinct().ToList(),
            generated, checked((int)cost), required).IsAmbiguous();

    private sealed class PaidResourceAmbiguitySearch(
        List<char> queried, string generated, int cost, string required)
    {
        private readonly Dictionary<char, (bool Paid, bool NotPaid)> outcomes = queried.ToDictionary(
            resource => resource,
            _ => (Paid: false, NotPaid: false));
        private readonly char[] selected = new char[cost];

        internal bool IsAmbiguous() => queried.Count > 0 && Search(start: 0, chosen: 0);

        private bool Search(int start, int chosen)
        {
            if (chosen == selected.Length)
            {
                return DeclareWild(selected.ToArray(), index: 0);
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

        private bool DeclareWild(char[] declared, int index)
        {
            while (index < declared.Length && declared[index] != Resources.Wild) index++;
            if (index < declared.Length)
            {
                foreach (char declaration in Resources.Types)
                {
                    declared[index] = declaration;
                    if (DeclareWild(declared, index + 1)) return true;
                }
                declared[index] = Resources.Wild;
                return false;
            }
            if (!PaysRequirement(declared)) return false;
            return Observe(declared);
        }

        private bool PaysRequirement(char[] declared)
        {
            var pool = declared.ToList();
            foreach (char requiredType in required)
            {
                int found = pool.IndexOf(requiredType);
                if (found < 0) return false;
                pool.RemoveAt(found);
            }
            return true;
        }

        private bool Observe(char[] declared)
        {
            foreach (char resource in queried)
            {
                bool paid = declared.Contains(resource);
                var seen = outcomes[resource];
                outcomes[resource] = paid ? (true, seen.NotPaid) : (seen.Paid, true);
                if (outcomes[resource] is (true, true)) return true;
            }
            return false;
        }
    }

    private static string DeclaredPaidResources(
        string generated, long cost, string required) =>
        new PaidResourceDeclarationSearch(generated, checked((int)cost), required).Find();

    private sealed class PaidResourceDeclarationSearch(
        string generated, int cost, string required)
    {
        private readonly char[] selected = new char[cost];
        private string? declaredPayment;

        internal string Find()
        {
            Search(start: 0, chosen: 0);
            return declaredPayment
                ?? throw new RulesNotImplementedException(
                    "the generated resources have no legal declared payment");
        }

        private bool Search(int start, int chosen)
        {
            if (chosen == selected.Length)
            {
                var declared = selected.ToArray();
                return DeclareWild(index: 0);
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

        private bool DeclareWild(int index)
        {
            var declared = selected.ToArray();
            return DeclareWild(declared, index);
        }

        private bool DeclareWild(char[] declared, int index)
        {
            while (index < declared.Length && declared[index] != Resources.Wild) index++;
            if (index < declared.Length)
            {
                foreach (char declaration in Resources.Types)
                {
                    declared[index] = declaration;
                    if (DeclareWild(declared, index + 1)) return true;
                }
                declared[index] = Resources.Wild;
                return false;
            }
            if (!SatisfiesRequired(declared)) return false;
            declaredPayment = new string(declared);
            return true;
        }

        private bool SatisfiesRequired(char[] declared)
        {
            var pool = declared.ToList();
            foreach (char requiredType in required)
            {
                int found = pool.IndexOf(requiredType);
                if (found < 0) return false;
                pool.RemoveAt(found);
            }
            return true;
        }
    }

}
