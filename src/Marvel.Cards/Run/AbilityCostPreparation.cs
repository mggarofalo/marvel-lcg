using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityPaymentInstructions;

namespace Marvel.Cards.Run;

/// <summary>Validates arrow costs and binds the exact payment instructions before commitment.</summary>
internal sealed class AbilityCostPreparation
{
    private readonly World world;
    private readonly Card source;
    private readonly int player;
    private readonly ImmutableArray<int> paying;
    private readonly ImmutableArray<int> selected;
    private readonly AbilityProgram program;
    private readonly IResourceCardAbilities resourceAbilities;
    private readonly ImmutableDictionary<string, long>? variables;
    private readonly bool resourcesPaidByEvent;
    private readonly int incarnation;
    private readonly AbilityPaymentSelection selection;
    private Marvel.Rules.Prompts.CostOption? resourcePrice;
    private IReadOnlyList<ResourceAllocation> effectiveAllocations = [];
    private AbilityCost? completeCost;
    private readonly ImmutableArray<Step>.Builder ordered =
        ImmutableArray.CreateBuilder<Step>();

    internal AbilityCostPreparation(
        World world, Card source, int player, AbilityProgram program,
        IResourceCardAbilities resourceAbilities, AbilityPaymentSelection selection)
    {
        this.world = world;
        this.selection = selection;
        this.source = source;
        this.player = player;
        paying = selection.Paying;
        selected = selection.Chosen;
        this.program = program;
        this.resourceAbilities = resourceAbilities;
        variables = selection.Values;
        resourcesPaidByEvent = selection.ResourcesPaidByEvent;
        incarnation = source.Incarnation;
        effectiveAllocations = selection.Allocations;
    }

    internal AbilityCostPayment Build(AbilityCost? cost)
    {
        cost = AbilityInvocationPayment.Expand(world, player, cost);
        completeCost = cost;
        AttachedUpgradeCostBound.Validate(world, source, player, cost, paying);
        var price = AbilityPaymentPricing.Price(world, source, player, cost, resourceAbilities);
        PrepareResourcePrice(cost, price);
        PrepareSteps(cost);
        return new AbilityCostPayment(
            world, source, player, resourceAbilities, paying, ordered.ToImmutable());
    }

    private void PrepareResourcePrice(AbilityCost? cost, Marvel.Rules.Prompts.CostOption? price)
    {
        ValidateDiscardSourceCost(cost);
        if (!resourcesPaidByEvent && price is not null
            && price.ResourceCosts.Any(component => component.DistinctTypes)
            && !Marvel.Rules.Prompts.ResourcePayment.Allows(price, paying, variables, selection.Allocations))
            throw new RulesNotImplementedException("the selected allocation does not pay the distinct resource cost");
        bool conditional = PrepareConditionalAllocations(price);
        resourcePrice = conditional || price?.ResourceCosts.Any(component => component.DistinctTypes) == true ? price : null;
    }

    private void ValidateDiscardSourceCost(AbilityCost? cost)
    {
        // Engine choice: a standalone discard-source arrow cannot consume resource generators.
        // Events retain their separately validated printed-price payment.
        if (cost is AbilityCost.Discard { Card: AbilityCostCard.Source } && paying.Length > 0
            && EffectiveCards.Kind(source, world.Facts) != CardKind.Event)
            throw new RulesNotImplementedException("this ability has no resource cost to pay");
    }

    private bool PrepareConditionalAllocations(Marvel.Rules.Prompts.CostOption? price)
    {
        bool conditional = price?.Generators.Any(generator => paying.Contains(generator.Effect)
            && generator.ConditionalBonus is not null) == true;
        if (!resourcesPaidByEvent && conditional && effectiveAllocations.Count == 0)
            effectiveAllocations = Marvel.Rules.Prompts.ResourcePayment.AllocateUnambiguousGeneration(price!, paying, variables)
                ?? throw new RulesNotImplementedException("conditional resource generation requires the player's allocation");
        if (!resourcesPaidByEvent && conditional && !Marvel.Rules.Prompts.ResourcePayment.Allows(
                price!, paying, variables, effectiveAllocations))
            throw new RulesNotImplementedException("the selected allocation does not pay the resource cost");
        return conditional;
    }

    private BoundCard Bind(AbilityCostCard binding) => binding switch
    {
        AbilityCostCard.Source => new(source, incarnation),
        AbilityCostCard.Identity when player >= 0 =>
            new(world.Seats[player].IdentityCard, world.Seats[player].IdentityCard.Incarnation),
        _ => throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no resolving player for its cost target"),
    };

    private void PrepareSteps(AbilityCost? component)
    {
        var validationAllocations = ValidationAllocations();
        new AbilityPaymentValidation(world, source, player, program, resourceAbilities)
            .Validate(component, paying, selected, variables, completeCost, validationAllocations);
        if (component is null or AbilityCost.ChooseFromDiscard or AbilityCost.ChooseScheme) return;
        if (component is AbilityCost.Sequence sequence)
        {
            PrepareSequence(sequence);
            return;
        }
        if (component is AbilityCost.Spend or AbilityCost.SpendEnergy)
        {
            PrepareResource(component);
            return;
        }
        ValidateBasicUse(component);
        PreparePhysical(component);
    }

    private void ValidateBasicUse(AbilityCost component)
    {
        if (component is AbilityCost.ExhaustChosen { CaptureBasicUsePower: true }
            && (world.Agenda.Occurrence?.BasicUse is not { } use || !use.IsCurrent(world)
                || selected.Length != 1 || BasicUseAllyReceipt.Power(world, world.Cards[selected[0]], use) <= 0))
            throw new RulesNotImplementedException("matching basic power requires a current positive selected ally power");
    }

    private IReadOnlyList<ResourceAllocation> ValidationAllocations()
    {
        // The event's shared allocation is validated separately before commitment.
        // Both components belong to this card; the arrow-only validation has one component.
        IReadOnlyList<ResourceAllocation> validationAllocations = resourcesPaidByEvent
            ? effectiveAllocations.Select(allocation => allocation with { Cost = 0 }).ToArray()
            : effectiveAllocations;
        if (resourcesPaidByEvent && validationAllocations.Count == 0)
            validationAllocations = CardPayment.Generators(world, world.Facts, world.Seats[player], resourceAbilities,
                    abilitySource: source).Where(generator => paying.Contains(generator.Effect)
                        && generator.ConditionalBonus is not null)
                .Select(generator => new ResourceAllocation(generator.Effect, 0, "G")).ToArray();
        return validationAllocations;
    }

    private void PrepareSequence(AbilityCost.Sequence sequence)
    {
        var components = AbilityCostOrder.Flatten(sequence).ToArray();
        // Engine choice: establish the taken-damage amount before other physical
        // payment. rr:cost.12 requires that "all of that damage was taken."
        // Defeat settlement waits until the combined costs have been committed.
        foreach (var taking in components.Where(AbilityCostOrder.IsTaking))
            PrepareSteps(taking);
        var spends = components.OfType<AbilityCost.Spend>().ToList();
        if (!resourcesPaidByEvent && spends.Count > 0)
            ordered.Add(new Spend(string.Concat(spends.Select(step => step.Resources)),
                Generic: spends.Sum(step => step.Generic))
                { Price = resourcePrice, Allocations = effectiveAllocations.ToImmutableArray() });
        foreach (var remaining in components.Where(step =>
                     step is not (AbilityCost.Spend
                         or AbilityCost.Damage { MustTakeAll: true })))
            PrepareSteps(remaining);
    }

    private void PrepareResource(AbilityCost component)
    {
        if (resourcesPaidByEvent) return;
        if (component is AbilityCost.Spend spend)
        {
            ordered.Add(new Spend(spend.Resources, Generic: spend.Generic)
                { Price = resourcePrice, Allocations = effectiveAllocations.ToImmutableArray() });
            return;
        }
        long x = AbilityPaymentValidation.DefinedVariable(variables, "X", source);
        var resolvedPrice = resourcePrice is null ? null : resourcePrice with
        {
            Cost = x.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Variables = null,
            Components = [new Marvel.Rules.Prompts.ResourceCost(
                x.ToString(System.Globalization.CultureInfo.InvariantCulture))
                { RepeatedResource = Resources.Energy }],
        };
        ordered.Add(new Spend(
            new string(Resources.Energy, checked((int)x)), Energy: x)
            { Price = resolvedPrice, Allocations = effectiveAllocations.ToImmutableArray() });
    }

    private void PrepareDamage(AbilityCost.Damage damage) =>
        ordered.Add(damage.MustTakeAll
            ? new TakeDamage(Bind(damage.Card), damage.Amount)
            : new DealDamage(Bind(damage.Card), damage.Amount));

    private bool PrepareHandDiscard(AbilityCost component)
    {
        switch (component)
        {
            case AbilityCost.RandomDiscardFromHand random:
                ordered.Add(new RandomDiscardHand(world.Seats[player].Hand,
                    [.. AbilityCostSelection.HandCandidates(world, source, player)
                        .Select(card => new BoundCard(card, card.Incarnation))], random.Count));
                return true;
            case AbilityCost.DiscardFromHand:
                ordered.Add(new DiscardHand(world.Seats[player].Hand,
                    [.. selected.Select(id => new BoundCard(world.Cards[id], world.Cards[id].Incarnation))]));
                return true;
            default: return false;
        }
    }

    private void PreparePhysical(AbilityCost component)
    {
        if (PrepareHandDiscard(component)) return;
        switch (component)
        {
            case AbilityCost.DiscardUntilMinionEngaged or AbilityCost.DiscardTop or AbilityCost.DealEncounterToYou:
                ordered.Add(new DeckCost(component));
                break;
            case AbilityCost.ExhaustChosen exhaustChosen:
                ordered.Add(new ExhaustSelected(
                    [.. selected.Select(id => new BoundCard(world.Cards[id], world.Cards[id].Incarnation))], exhaustChosen.CaptureBasicUsePower));
                break;
            case AbilityCost.ExhaustTitled named:
                var target = NamedAbilityCost.RequireTarget(world, player, named.Title);
                ordered.Add(new Exhaust(new BoundCard(target, target.Incarnation)));
                break;
            case AbilityCost.Exhaust exhaust:
                ordered.Add(new Exhaust(Bind(exhaust.Card)));
                break;
            case AbilityCost.ConsumeCampaignCard or AbilityCost.Discard
                or AbilityCost.ReturnSourceToHand or AbilityCost.ReturnTitledToHand:
                ordered.Add(PrepareDeparture(component));
                break;
            case AbilityCost.RemoveCounters counters:
                ordered.Add(new RemoveCounters(
                    Bind(counters.Card), counters.Counter, counters.Count));
                break;
            case AbilityCost.Heal heal:
                ordered.Add(new Heal(Bind(heal.Card), heal.Amount));
                break;
            case AbilityCost.Damage damage:
                PrepareDamage(damage);
                break;
            default:
                throw new InvalidOperationException(
                    "Unknown compiled cost in payment preparation");
        }
    }

    private BoundCard BindNamed(string title)
    {
        Card target = NamedAbilityCost.RequireTarget(world, player, title);
        return new BoundCard(target, target.Incarnation);
    }

    private Step PrepareDeparture(AbilityCost component) => component switch
    {
        AbilityCost.ConsumeCampaignCard consume => new ConsumeCampaignBound(Bind(consume.Card)),
        AbilityCost.Discard discard => new DiscardBound(Bind(discard.Card)),
        AbilityCost.ReturnSourceToHand => new ReturnBound(Bind(AbilityCostCard.Source)),
        AbilityCost.ReturnTitledToHand named => new ReturnBound(BindNamed(named.Title)),
        _ => throw new InvalidOperationException("Unknown departure cost"),
    };
}
