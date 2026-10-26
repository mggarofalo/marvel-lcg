using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns optional prevention for one exact nonattack damage recipient.</summary>
public sealed class DirectDamagePrevention
{
    private readonly Card target;
    private readonly int incarnation;
    private Damage.PlacedDamage placement;
    internal DirectDamagePrevention(World world, DamageRequest request, DamageReplacement replacement,
        Damage.PlacedDamage placement)
    {
        target = request.Target;
        incarnation = target.Incarnation;
        this.placement = placement;
        Occurrence = new(world.Agenda.nextPlayerActionOccurrence, [Steps.DamageWouldBeDealt],
            Subject: target.ObjectId, Player: target.Owner, Actor: request.Source.ObjectId,
            Target: target.ObjectId, ActorFacts: OccurrenceCard.Capture(request.Source, world.Facts),
            TargetFacts: OccurrenceCard.Capture(target, world.Facts))
        { Damage = replacement, DirectDamage = this };
    }
    internal Occurrence Occurrence { get; }
    internal bool IsComplete { get; private set; }
    /// <summary>The damage still imminent for the captured in-play copy.</summary>
    public long Remaining => IsCurrent ? placement.Taken : 0;
    private bool IsCurrent => target.Incarnation == incarnation && DeckTypes.IsInPlay(target.Area.Type);
    internal Damage.PlacedDamage Placement => IsCurrent ? placement : new(target, 0, 0);

    /// <summary>Prevent damage only from an active invocation answering this exact occurrence.</summary>
    public void Prevent(World world, long amount)
    {
        var invocation = world.Agenda.Executions.Active
            ?? throw new InvalidOperationException("damage prevention requires an active invocation");
        world.Agenda.Executions.RequireActive(invocation);
        if (IsComplete || !ReferenceEquals(invocation.Occurrence, Occurrence))
            throw new InvalidOperationException("the ability does not own this damage prevention");
        placement = placement with { Taken = Math.Max(0, Remaining - (amount <= 0 ? Remaining : amount)) };
    }
    internal static DirectDamagePrevention Begin(World world, DamageRequest request,
        DamageReplacement replacement, Damage.PlacedDamage placement)
    {
        var prevention = new DirectDamagePrevention(world, request, replacement, placement);
        // rr:damage.step.2 precedes step.3: tough and zero damage leave no taking condition.
        if (prevention.Remaining <= 0 || new OptionalDamageInterrupts(world.WindowAbilities)
            .Waiting(world, prevention.Occurrence, WindowKind.Interrupt).Count == 0)
            prevention.IsComplete = true;
        else
        {
            world.Agenda.nextPlayerActionOccurrence--;
            world.Agenda.Then(new(new DirectDamagePreventionOperation(prevention),
                world.Agenda.Current?.Round ?? 0, 3, Subject: request.Target.ObjectId, Seat: request.Target.Owner));
        }
        return prevention;
    }
    internal void Complete() => IsComplete = true;
}
