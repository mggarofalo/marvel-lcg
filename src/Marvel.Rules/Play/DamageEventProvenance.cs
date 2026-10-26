using Marvel.Rules.Events;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Retains the initial forced receipt for a damage assignment's resumed segment.</summary>
internal sealed class DamageEventProvenance(AbilityExecution execution)
{
    private readonly AbilityExecution? owner = FindOwner(execution);

    internal void Associate(List<GameEvent> events, int start)
    {
        if (owner is null) return;
        owner.DamagePreventionSuspended = true;
        // Engine-chosen association matches the initial forced receipt. Only this
        // damage segment is included; nested prevention runs before it resumes.
        for (int index = start; index < events.Count; index++)
            if (events[index].AbilitySource is null)
                events[index] = events[index] with
                { AbilitySource = owner.Ability.Card, AbilityOccurrence = owner.Occurrence.Id,
                    AbilityTiming = owner.Ability.Type, AbilityOrdinal = owner.Ability.Ordinal };
    }

    private static AbilityExecution? FindOwner(AbilityExecution execution)
    {
        var owner = execution;
        while (owner.IsBatch && owner.Parent is { } parent) owner = parent;
        // An individual optional invocation owns its damage even inside a forced ancestor.
        return !owner.IsBatch && owner.Ability.Type is AbilityType.ForcedInterrupt or AbilityType.ForcedResponse
            ? owner : null;
    }
}
