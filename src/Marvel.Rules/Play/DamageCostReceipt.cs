using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Fixed damage paid as a cost, followed by explicitly released defeat settlement.</summary>
public sealed class DamageCostReceipt
{
    private readonly World world;
    private readonly ICardFacts facts;
    private readonly DamageRequest request;
    private readonly AbilityExecution owner;
    private readonly AbilityCompletionObligation obligation;
    private Damage.PlacedDamage? placement;
    private DamageResult? settlement;
    private bool settling;

    internal DamageCostReceipt(World world, ICardFacts facts, DamageRequest request,
        AbilityExecution owner, AbilityExecution placementExecution, AbilityCompletionObligation obligation)
    {
        this.world = world;
        this.facts = facts;
        this.request = request;
        this.owner = owner;
        this.obligation = obligation;
        PlacementExecution = placementExecution;
    }

    /// <summary>The stable child execution that fixes replacement and places damage.</summary>
    public AbilityExecution PlacementExecution { get; }
    /// <summary>Whether the paid damage amount is fixed and available.</summary>
    public bool IsPlaced => placement is not null && PlacementExecution.IsComplete;
    /// <summary>The actual amount paid, after prevention.</summary>
    public long Taken => Placed.Taken;
    /// <summary>The dealt amount, after replacement.</summary>
    public long Dealt => Placed.Dealt;
    /// <summary>Whether defeat settlement has been started by the owner.</summary>
    public bool IsSettling => settling;
    /// <summary>Whether both placement and all resulting defeat work have completed.</summary>
    public bool IsComplete => settlement?.IsComplete == true;
    private Damage.PlacedDamage Placed => IsPlaced ? placement!
        : throw new InvalidOperationException("the damage cost has not reached placement");

    internal void Place(Damage.PlacedDamage value) => placement = value;

    /// <summary>Release defeat after simultaneous costs have been paid; only the exact active cost owner may do so.</summary>
    public void Settle(AbilityExecution invocation, List<GameEvent> events) =>
        DamageCostSettlement.Settle(world, invocation, [this], events);

    internal Damage.PlacedDamage ValidateSettlement(World requestingWorld, AbilityExecution invocation)
    {
        world.Agenda.Executions.RequireActive(invocation);
        if (!ReferenceEquals(world, requestingWorld) || !ReferenceEquals(owner, invocation))
            throw new InvalidOperationException("the invocation does not own this damage cost");
        var damage = Placed;
        if (settling) throw new InvalidOperationException("the damage cost was already settled");
        return damage;
    }

    internal DamageResult BeginSettlement(Damage.PlacedDamage damage, List<GameEvent> events)
    {
        DamageResult? result = null;
        world.Agenda.Executions.RunBatch(request.RecordDefeatOn ?? owner.Occurrence, execution =>
        {
            var resolution = new DamageResolution(world, facts, request, execution);
            result = resolution.Result;
            resolution.Settle(damage, events);
            return execution.Report(events);
        });
        return result!;
    }

    internal void AcceptSettlement(DamageResult result)
    {
        settling = true;
        settlement = result;
        obligation.Release();
    }
}
