using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Physical villain/side-scheme relations and threat-ranked encounter targets.</summary>
public static class EncounterSchemeRelations
{
    /// <summary>The living villain's corresponding physical scheme, independent of the active counter.</summary>
    public static Card Scheme(World world, Card villain)
    {
        var group = Topology(world).Groups.SingleOrDefault(candidate => candidate.Stages.Contains(villain));
        if (group is null || !ReferenceEquals(group.CurrentVillain, villain))
            throw new RulesNotImplementedException($"'{villain.FaceId}' requires its living corresponding side scheme");
        return LiveScheme(group);
    }

    /// <summary>The current physical villain corresponding to this live scheme.</summary>
    public static Card Villain(World world, Card scheme)
    {
        var group = Topology(world).Groups.SingleOrDefault(candidate => ReferenceEquals(candidate.Scheme, scheme));
        if (group is null || scheme.Area.Type != DeckType.SideSchemesArea || group.CurrentVillain is null)
            throw new RulesNotImplementedException($"'{scheme.FaceId}' requires its living corresponding villain");
        return group.CurrentVillain;
    }

    /// <summary>Every living villain tied at the global scheme-threat extreme, in setup order.</summary>
    public static IReadOnlyList<Card> AtThreatExtreme(World world, bool maximum)
    {
        var living = Topology(world).Groups.Where(group => group.CurrentVillain is not null).ToArray();
        if (living.Length == 0) return [];
        var values = living.Select(group => LiveScheme(group).Tokens.GetValueOrDefault("k_threat")).ToArray();
        long extreme = maximum ? values.Max() : values.Min();
        return living.Where((_, index) => values[index] == extreme).Select(group => group.CurrentVillain!).ToArray();
    }

    /// <summary>Rank first, then exclude the current placement as required by rr:move.1.</summary>
    public static IReadOnlyList<Card> CounterDestinations(World world, bool maximum)
    {
        // "When an element moves, it cannot move to its same (current) placement."
        // A unique extreme at the current holder leaves no legal destination;
        // it does not make the next-ranked villain eligible.
        var active = Topology(world).Active.CurrentVillain;
        return AtThreatExtreme(world, maximum).Where(card => !ReferenceEquals(card, active)).ToArray();
    }

    private static Card LiveScheme(EncounterDeckGroup group) =>
        group.Scheme.Area.Type == DeckType.SideSchemesArea ? group.Scheme
            : throw new RulesNotImplementedException($"'{group.Villain.FaceId}' requires its living corresponding side scheme");

    private static EncounterTopology Topology(World world) =>
        world.EncounterTopology ?? throw new RulesNotImplementedException("corresponding encounter schemes require encounter topology");
}
