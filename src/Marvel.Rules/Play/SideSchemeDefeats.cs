using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Owns simultaneous side-scheme defeat abilities and their final departures.</summary>
internal static class SideSchemeDefeats
{
    internal static bool Resolve(World world, IReadOnlyList<ThreatRemovalRequest> requests, List<GameEvent> events)
    {
        if (requests.Count == 0) return false;
        var first = requests[0];
        if (requests.Count == 1)
            return Defeat.Scheme(world, world.Facts, first.Scheme, first.Trigger, events, first.By,
                first.RecordDefeatOn) == Damage.Outcome.Suspended;

        // rr:simultaneous-resolution: the first player orders effects with the same bold timing trigger.
        // Derived timing: one interrupt window for the simultaneous conditions (rr:triggering-condition.2)
        // finishes before departures, which occur after When Defeated abilities (rr:when-defeated-abilities.2.1).
        var defeats = requests.Select(request => Defeat.Record(world, request.Scheme, request.By,
            BasicPowers.ThwartVerb, request.RecordDefeatOn)).ToArray();
        var owner = world.Agenda.Current;
        var containing = world.Agenda.Occurrence;
        var result = world.DamageAbilities.ExecuteDefeats(world, defeats, first.Trigger);
        ActivityBatches.Append(events, result.Events);
        foreach (var request in requests)
        {
            if (result.IsComplete)
                Defeat.FinalizeScheme(world, world.Facts, request.Scheme, request.Trigger, events);
            else
                DefeatScheduling.Finalize(world, owner, containing, request.Scheme,
                    new DefeatCompletionOperation(DefeatCompletionKind.FinalizeSchemeDefeat,
                        request.Trigger, request.Scheme.Incarnation), result.Invocation);
        }
        return !result.IsComplete;
    }
}
