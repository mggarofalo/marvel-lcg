using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Starts an owned simultaneous attack-damage procedure.</summary>
public static class SimultaneousAttackDamage
{
    /// <summary>Resolve distinct recipients through one simultaneous damage-placement barrier.</summary>
    public static SimultaneousAttackDamageResult Resolve(World world, ICardFacts facts,
        SimultaneousAttackDamageRequest request, List<GameEvent> events)
    {
        if (request.Targets.Count == 0 || request.Targets.DistinctBy(card => card.ObjectId).Count() != request.Targets.Count)
            throw new ArgumentException("simultaneous attack damage requires distinct targets", nameof(request));
        SimultaneousAttackDamageResolution? resolution = null;
        AbilityExecutionResult Apply(Occurrence cause, AbilityExecution? completion) =>
            world.Agenda.Executions.RunBatch(cause, entry =>
            {
                resolution = new(world, facts, request, entry, completion);
                resolution.Continue(events);
                return entry.Report(events);
            });
        if (world.Agenda.Occurrence is { } occurrence) Apply(occurrence, null);
        else RootDamageExecution.Run(world, request.Attacker, request.Targets[0], Apply);
        return resolution!.Result;
    }

}
