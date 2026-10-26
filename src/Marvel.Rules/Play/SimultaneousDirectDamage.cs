using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves direct damage to distinct recipients through a shared placement barrier.</summary>
public static class SimultaneousDirectDamage
{
    /// <summary>Complete all replacements and placements before settling any recipient's defeat.</summary>
    public static SimultaneousDirectDamageResult Resolve(World world, ICardFacts facts,
        IReadOnlyList<DamageRequest> requests, List<GameEvent> events)
    {
        if (requests.Count == 0 || requests.DistinctBy(item => item.Target.ObjectId).Count() != requests.Count)
            throw new ArgumentException("simultaneous direct damage requires distinct targets", nameof(requests));
        SimultaneousDirectDamageResolution? resolution = null;
        AbilityExecutionResult Apply(Occurrence cause, AbilityExecution? completion) =>
            world.Agenda.Executions.RunBatch(cause, entry =>
            {
                resolution = new(world, facts, requests, entry, completion);
                resolution.Continue(events);
                return entry.Report(events);
            });
        if ((requests[0].RecordDefeatOn ?? world.Agenda.Occurrence) is { } cause) Apply(cause, null);
        else RootDamageExecution.Run(world, requests[0].Source, requests[0].Target, Apply);
        return resolution!.Result;
    }
}
