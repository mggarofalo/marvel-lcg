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
    }

    internal AbilityCostPayment Build(AbilityCost? cost)
    {
        AttachedUpgradeCostBound.Validate(world, source, player, cost, paying);
        var price = AbilityPaymentPricing.Price(world, source, player, cost, resourceAbilities);
        // Engine choice: a standalone discard-source arrow cannot consume resource generators.
        // Events retain their separately validated printed-price payment.
        if (cost is AbilityCost.Discard { Card: AbilityCostCard.Source } && paying.Length > 0
            && EffectiveCards.Kind(source, world.Facts) != CardKind.Event)
            throw new RulesNotImplementedException("this ability has no resource cost to pay");
        if (!resourcesPaidByEvent && price is not null
            && price.ResourceCosts.Any(component => component.DistinctTypes)
            && !Marvel.Rules.Prompts.ResourcePayment.Allows(price, paying, variables, selection.Allocations))
            throw new RulesNotImplementedException("the selected allocation does not pay the distinct resource cost");
        resourcePrice = price?.ResourceCosts.Any(component => component.DistinctTypes) == true ? price : null;
        PrepareSteps(cost);
        return new AbilityCostPayment(
            world, source, player, resourceAbilities, paying, ordered.ToImmutable());
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
        new AbilityPaymentValidation(world, source, player, program, resourceAbilities)
            .Validate(component, paying, selected, variables);
        if (component is null or AbilityCost.ChooseFromDiscard) return;
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
        if (component is AbilityCost.ExhaustChosen { CaptureBasicUsePower: true }
            && (world.Agenda.Occurrence?.BasicUse is not { } use || !use.IsCurrent(world)
                || selected.Length != 1 || BasicUseAllyReceipt.Power(world, world.Cards[selected[0]], use) <= 0))
            throw new RulesNotImplementedException("matching basic power requires a current positive selected ally power");
        PreparePhysical(component);
    }

    private void PrepareSequence(AbilityCost.Sequence sequence)
    {
        var components = Flatten(sequence).ToArray();
        // Engine choice: establish the taken-damage amount before other physical
        // payment. rr:cost.12 requires that "all of that damage was taken."
        // Defeat settlement waits until the combined costs have been committed.
        foreach (var taking in components.OfType<AbilityCost.Damage>()
                     .Where(damage => damage.MustTakeAll))
            PrepareSteps(taking);
        var spends = components.OfType<AbilityCost.Spend>().ToList();
        if (!resourcesPaidByEvent && spends.Count > 0)
            ordered.Add(new Spend(string.Concat(spends.Select(step => step.Resources)),
                Generic: spends.Sum(step => step.Generic))
                { Price = resourcePrice, Allocations = selection.Allocations });
        foreach (var remaining in components.Where(step =>
                     step is not (AbilityCost.Spend
                         or AbilityCost.Damage { MustTakeAll: true })))
            PrepareSteps(remaining);
    }

    private static IEnumerable<AbilityCost> Flatten(AbilityCost cost)
    {
        if (cost is AbilityCost.Sequence sequence)
        {
            foreach (var child in sequence.Costs)
                foreach (var component in Flatten(child)) yield return component;
        }
        else yield return cost;
    }

    private void PrepareResource(AbilityCost component)
    {
        if (resourcesPaidByEvent) return;
        if (component is AbilityCost.Spend spend)
        {
            ordered.Add(new Spend(spend.Resources, Generic: spend.Generic)
                { Price = resourcePrice, Allocations = selection.Allocations });
            return;
        }
        long x = AbilityPaymentValidation.DefinedVariable(variables, "X", source);
        ordered.Add(new Spend(
            new string(Resources.Energy, checked((int)x)), Energy: x));
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
