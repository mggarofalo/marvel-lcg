using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Validates an engine-owned entry before authored abilities can read or mutate its game.</summary>
internal static class AbilityEntryExecution
{
    internal static AbilityExecutionResult Run(World world, AbilityExecution entry,
        Func<IReadOnlyList<GameEvent>> body)
    {
        world.Agenda.Executions.RequireBatch(entry);
        return entry.Finish(body());
    }
}
