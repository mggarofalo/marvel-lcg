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
    private readonly CostOption? price;
    private readonly ImmutableArray<ResourceAllocation> allocations;
    private ResourcePaymentReceipt? payment;
    private bool completed;
    internal long? Energy { get; private init; }

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
        price = quote.Price;
        allocations = quote.Allocations;
    }

    internal static AbilityEventPayment? Prepare(
        World world, Card card, int player, CompiledCardAbility ability,
        IResourceCardAbilities resourceAbilities, AbilityPaymentSelection selection, ScopedCardCostReduction? reduction = null)
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

        var paying = selection.Paying;
        var allocations = selection.Allocations;
        long? energy = ValidateVariableEnergy(world, card, player, ability, resourceAbilities, selection, reduction);
        var adjusted = CardPayment.CostOf(
            world, world.Facts, world.Seats[player], card, attachmentHost: null, reduction: reduction);
        var sources = EventPaymentSources.Find(world, card, player, resourceAbilities);
        ValidateSelectedSources(card, paying, sources.Generators);

        var requirements = EventPaymentRequirements.Create(
            world, card, adjusted, AbilityInvocationPayment.Expand(world, player, ability.Cost), energy, ability);
        var price = AbilityPaymentPricing.CombinedPrice(world, card, player, ability, resourceAbilities, reduction)!;
        price = RestrictSingleComponent(price, requirements);
        var resolved = ResolveAllocations(card, price, requirements, paying, allocations);
        var generatedSources = ResolveSources(price.Generators, paying, resolved, requirements.Total);
        string generated = GeneratedResources(generatedSources, paying);
        ValidateGeneratedPayment(generated, requirements);
        if (allocations.Length == 0 && HasAmbiguousPaidResourceAllocation(
                ability.Effect, generated, requirements.Total, requirements.Required))
            throw new RulesNotImplementedException("the paid resource declaration requires the player's allocation");

        var assigned = resolved;
        string paid = ResolvePaidResources(
            card, ability.Effect, generatedSources, paying, assigned, generated, requirements);

        return new AbilityEventPayment(world, card, player, resourceAbilities, paying, sources,
            new EventPaymentQuote(adjusted, requirements.Total, requirements.Required, paid,
                assigned.Count == 0 && !price.Generators.Any(source => paying.Contains(source.Effect)
                    && source.ConditionalBonus is not null) ? null : new CostOption(card.ObjectId,
                    requirements.Total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Sources: [.. price.Generators], Components: [.. requirements.Components]),
                [.. assigned])) { Energy = energy };
    }

    private static CostOption RestrictSingleComponent(CostOption price, EventPaymentRequirements requirements)
    {
        if (requirements.Components.Count == 1)
            price = price with { Sources = [.. price.Generators.Select(source => source.ConditionalBonus is { } bonus
                ? source with { ConditionalBonus = (bonus.ComponentMask & 1) == 0 ? null
                    : bonus with { ComponentMask = 1 } }
                : source)] };
        return price;
    }

    private static IReadOnlyList<ResourceAllocation> ResolveAllocations(
        Card card, CostOption price, EventPaymentRequirements requirements,
        IReadOnlyList<int> paying, IReadOnlyList<ResourceAllocation> allocations)
    {
        IReadOnlyList<ResourceAllocation> resolved = allocations;
        if (resolved.Count == 0 && price.Generators.Any(source => paying.Contains(source.Effect)
                && source.ConditionalBonus is not null))
        {
            var fixedPrice = new CostOption(card.ObjectId,
                requirements.Total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Sources: price.Generators, Components: requirements.Components);
            resolved = ResourcePayment.AllocateUnambiguousGeneration(fixedPrice, paying)
                ?? throw new RulesNotImplementedException("conditional resource generation requires the player's allocation");
        }
        return resolved;
    }

    private static long? ValidateVariableEnergy(
        World world, Card card, int player, CompiledCardAbility ability,
        IResourceCardAbilities resourceAbilities, AbilityPaymentSelection selection, ScopedCardCostReduction? reduction)
    {
        long? energy = null;
        if (ability.Cost is AbilityCost.SpendEnergy)
        {
            energy = AbilityPaymentValidation.DefinedVariable(selection.Values, "X", card);
            var variablePrice = AbilityPaymentPricing.CombinedPrice(world, card, player, ability, resourceAbilities, reduction)!;
            if (!variablePrice.VariableRequests.Single().Allows(energy.Value)
                || !ResourcePayment.Allows(variablePrice, selection.Paying, selection.Values, selection.Allocations))
                throw new RulesNotImplementedException("the allocation does not pay the printed and variable energy costs");
        }
        return energy;
    }

    private sealed record EventPaymentQuote(AdjustedCardCost Adjusted, long Total, string Required, string Paid,
        CostOption? Price, ImmutableArray<ResourceAllocation> Allocations);

    private static ResourceSource[] ResolveSources(
        IReadOnlyList<ResourceSource> sources, IReadOnlyList<int> paying,
        IReadOnlyList<ResourceAllocation> allocations, long amount)
    {
        if (allocations.Count == 0 && amount > 0
            && sources.Any(source => paying.Contains(source.Effect) && source.ConditionalBonus is not null))
            throw new RulesNotImplementedException("conditional resource generation requires the player's allocation");
        return [.. sources.Select(source => source with
        {
            Generates = ResourcePayment.GeneratedBy(source, allocations), ConditionalBonus = null,
        })];
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
        NextEventCost.Reserve(world, card, adjusted);
        CardPayment.UseCostModifiers(world, adjusted);
        // rr:initiating-abilities.step.6: the event is played after its costs are paid.
        if (occurrence.Is(Steps.TurnAction)) occurrence.Also(Steps.CardPlayed);
        completed = true;
        return true;
    }

    internal ResourcePaymentReceipt? ResourceReceipt => payment;

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

        payment = CardResourcePayments.Spend(world, resourceAbilities, new CardResourcePaymentRequest
        {
            Hands = hands, Paying = paying, Cost = total, Required = required, Itself = card.ObjectId,
            Payer = player, PayingFor = card, ResourcePayers = resourcePayers,
            Price = price, Allocations = allocations,
        }, events);
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
