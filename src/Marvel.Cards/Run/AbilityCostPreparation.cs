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
    private readonly ImmutableArray<Step>.Builder ordered =
        ImmutableArray.CreateBuilder<Step>();

    internal AbilityCostPreparation(
        World world, Card source, int player, AbilityProgram program,
        IResourceCardAbilities resourceAbilities, AbilityPaymentSelection selection)
    {
        this.world = world;
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
        if (component is null) return;
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
        PreparePhysical(component);
    }

    private void PrepareSequence(AbilityCost.Sequence sequence)
    {
        // Engine choice: establish the taken-damage amount before other physical
        // payment. rr:cost.12 requires that "all of that damage was taken."
        // Defeat settlement waits until the combined costs have been committed.
        foreach (var taking in sequence.Costs.OfType<AbilityCost.Damage>()
                     .Where(damage => damage.MustTakeAll))
            PrepareSteps(taking);
        var spends = sequence.Costs.OfType<AbilityCost.Spend>().ToList();
        if (!resourcesPaidByEvent && spends.Count > 0)
            ordered.Add(new Spend(string.Concat(spends.Select(step => step.Resources)),
                Generic: spends.Sum(step => step.Generic)));
        foreach (var remaining in sequence.Costs.Where(step =>
                     step is not (AbilityCost.Spend
                         or AbilityCost.Damage { MustTakeAll: true })))
            PrepareSteps(remaining);
    }

    private void PrepareResource(AbilityCost component)
    {
        if (resourcesPaidByEvent) return;
        if (component is AbilityCost.Spend spend)
        {
            ordered.Add(new Spend(spend.Resources, Generic: spend.Generic));
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

    private void PreparePhysical(AbilityCost component)
    {
        switch (component)
        {
            case AbilityCost.DiscardFromHand:
                ordered.Add(new DiscardHand(world.Seats[player].Hand,
                    [.. selected.Select(id => new BoundCard(world.Cards[id], world.Cards[id].Incarnation))]));
                break;
            case AbilityCost.ExhaustChosen:
                ordered.Add(new ExhaustSelected(
                    [.. selected.Select(id => new BoundCard(world.Cards[id], world.Cards[id].Incarnation))]));
                break;
            case AbilityCost.ExhaustTitled named:
                var target = NamedAbilityCost.RequireTarget(world, player, named.Title);
                ordered.Add(new Exhaust(new BoundCard(target, target.Incarnation)));
                break;
            case AbilityCost.Exhaust exhaust:
                ordered.Add(new Exhaust(Bind(exhaust.Card)));
                break;
            case AbilityCost.ConsumeCampaignCard or AbilityCost.Discard or AbilityCost.ReturnSourceToHand:
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

    private Step PrepareDeparture(AbilityCost component) => component switch
    {
        AbilityCost.ConsumeCampaignCard consume => new ConsumeCampaignBound(Bind(consume.Card)),
        AbilityCost.Discard discard => new DiscardBound(Bind(discard.Card)),
        AbilityCost.ReturnSourceToHand => new ReturnBound(Bind(AbilityCostCard.Source)),
        _ => throw new InvalidOperationException("Unknown departure cost"),
    };
}
