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

/// <summary>A validated event payment that completes after its resource abilities.</summary>
/// <remarks>Retains accepted payment facts while the owning ability is suspended.</remarks>
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
    private ResourcePaymentReceipt? payment;
    private bool completed;

    private AbilityEventPayment(
        World world, Card card, int player, IResourceCardAbilities resourceAbilities,
        IReadOnlyList<int> paying, EventPaymentSources sources, EventPaymentQuote quote)
    {
        this.world = world;
        this.card = card;
        this.player = player;
        this.resourceAbilities = resourceAbilities;
        this.paying = [.. paying];
        hands = [.. sources.PayingSeats.Select(seat => seat.Hand)];
        resourcePayers = sources.ResourcePayers.ToImmutableDictionary();
        adjusted = quote.Adjusted with { Modifiers = quote.Adjusted.Modifiers.ToImmutableArray() };
        total = quote.Total;
        required = quote.Required;
        paid = quote.Paid;
    }

    internal static AbilityEventPayment? Prepare(
        World world, Card card, int player, IReadOnlyList<int> paying, CompiledCardAbility ability,
        IResourceCardAbilities resourceAbilities,
        IReadOnlyList<ResourceAllocation>? allocations = null)
    {
        if (EffectiveCards.Kind(card, world.Facts) != CardKind.Event)
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
            world, card, adjusted, ability.Cost);
        ValidateGeneratedPayment(generated, requirements);

        var assigned = allocations ?? [];
        string paid = ResolvePaidResources(
            card, ability.Effect, sources.Generators, paying, assigned, generated, requirements);

        return new AbilityEventPayment(world, card, player, resourceAbilities, paying, sources,
            new EventPaymentQuote(adjusted, requirements.Total, requirements.Required, paid));
    }

    private sealed record EventPaymentQuote(AdjustedCardCost Adjusted, long Total, string Required, string Paid);

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
            int additionalAmount = checked(additional.Length + GenericResourceAmount(additionalCost));
            var components = new List<ResourceCost>
            {
                new(adjusted.Amount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                    printed.Length > 0 ? [printed] : null),
            };
            if (additionalAmount > 0)
                components.Add(new ResourceCost(
                    additionalAmount.ToString(
                        System.Globalization.CultureInfo.InvariantCulture), [additional]));
            // Total is an early affordability check. Component allocation independently
            // enforces the full amount, so omitting this addition is an equivalent
            // mutant when every component is checked by ResourceAllocationLedger.
            return new EventPaymentRequirements(
                checked(adjusted.Amount + additionalAmount), printed + additional, components);
        }
    }

    /// <summary>Commit the validated payment at the initiating boundary.</summary>
    internal bool Commit(Occurrence occurrence, List<GameEvent> events)
    {
        if (completed) return true;
        if (payment is null) Begin(events);
        if (!payment!.IsComplete) return false;

        foreach (char resource in paid.Distinct())
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect, Kind: "paid:" + resource,
                Card: card.ObjectId, Affects: card.ObjectId, Lasts: new Duration(Uses: 1)));
        CardPayment.UseCostModifiers(world, adjusted);
        // rr:initiating-abilities.step.6: the event is played after its costs are paid.
        if (occurrence.Is(Steps.TurnAction)) occurrence.Also(Steps.CardPlayed);
        completed = true;
        return true;
    }

    internal string PreviewPaid => paid;

    internal string Paid => completed ? paid
        : throw new InvalidOperationException("the event payment has not completed");

    private void Begin(List<GameEvent> events)
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

        payment = CardPlay.Spend(
            world, world.Facts, resourceAbilities, hands, paying,
            total,
            required, card.ObjectId,
            player, events, payingFor: card,
            resourcePayers: resourcePayers);
    }

    private static string AllocatedResources(
        IReadOnlyList<ResourceSource> generators,
        IReadOnlyList<int> paying,
        IReadOnlyList<ResourceAllocation> allocations,
        List<ResourceCost> components,
        Card card) => new ResourceAllocationLedger(
            generators, paying, components, card).Allocate(allocations);

    private static bool HasAmbiguousPaidResourceAllocation(
        AbilityEffect effect, string generated, long cost, string required) =>
        new PaidResourceAmbiguitySearch(
            PaidResourceQueries(effect).Distinct().ToList(),
            generated, checked((int)cost), required).IsAmbiguous();

    private static string DeclaredPaidResources(
        string generated, long cost, string required) =>
        new PaidResourceDeclarationSearch(generated, checked((int)cost), required).Find();

}
