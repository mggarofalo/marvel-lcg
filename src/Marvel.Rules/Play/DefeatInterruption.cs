using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Admits damage-step-six work and explicitly retains the interrupted defeat.</summary>
internal static class DefeatInterruption
{
    internal static bool Begin(World world, Card target, DefeatCause cause, List<GameEvent> events)
    {
        var owner = world.Agenda.Current;
        var containing = world.Agenda.Occurrence;
        int incarnation = target.Incarnation;
        var result = world.DamageAbilities.ExecuteWouldDefeat(world, target, cause);
        ActivityBatches.Append(events, result.Events);
        if (result.IsComplete) return false;
        DefeatScheduling.Finalize(world, owner, containing, target,
            new PendingDefeatOperation(cause, incarnation), result.Invocation);
        return true;
    }
}
