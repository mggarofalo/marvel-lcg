using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Applies simultaneous threat shares before opening their shared defeat window.</summary>
public static class ThreatBatchRemoval
{
    /// <summary>Returns each request's removal and the shared suspension boundary.</summary>
    public static IReadOnlyList<ThreatRemovalResult> Remove(World world, IThreatCardAbilities abilities,
        IReadOnlyList<ThreatRemovalRequest> requests, List<GameEvent> events)
    {
        // ruling:f216f6359fb0bf96: threat is removed from the schemes simultaneously.
        // Request order is the engine's event-stream spelling, not an ability order.
        var removed = requests.Select(request => Threat.Removable(world, abilities, request)).ToArray();
        for (int index = 0; index < requests.Count; index++)
            Threat.ApplyRemoval(world, requests[index], removed[index], events);
        var defeated = requests.Where((request, index) => removed[index] > 0
            && request.Scheme.Area.Type == DeckType.SideSchemesArea
            && request.Scheme.Tokens.GetValueOrDefault("k_threat") == 0
            && SideSchemeDeparture.Allowed(world, request.Scheme)).ToArray();
        bool suspended = SideSchemeDefeats.Resolve(world, defeated, events);
        return removed.Select(amount => new ThreatRemovalResult(amount, suspended)).ToArray();
    }
}
