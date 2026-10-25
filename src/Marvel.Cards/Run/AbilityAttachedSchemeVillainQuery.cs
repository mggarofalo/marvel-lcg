using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves the physical villain paired with the source attachment's scheme.</summary>
internal static class AbilityAttachedSchemeVillainQuery
{
    internal static IReadOnlyList<Card> Candidates(World world) =>
        Groups(world).SelectMany(group => group.Stages).ToArray();

    internal static IReadOnlyList<Card> Live(AbilityQueryContext context)
    {
        if (context.SourceReference is not { } source) return [];
        var group = AttachedGroup(context.World, source.Area.Host);
        return [EncounterSchemeRelations.Villain(context.World, group.Scheme)];
    }

    internal static IReadOnlyList<Card> InAreas(AbilityAdmissionContext context, AreaProjectionState state)
    {
        if (!state.SourceReferenceCurrent || state.Departed.Contains(context.Source.ObjectId)) return [];
        int host = state.Hosts.GetValueOrDefault(context.Source.ObjectId, context.Source.Area.Host);
        var group = AttachedGroup(context.World, host);
        if (state.Departed.Contains(group.Scheme.ObjectId)) return [];
        var villain = AbilityAreaEncounterProjection.Current(context.World, state, group.Villain);
        return villain is null ? [] : [villain];
    }

    internal static IReadOnlyList<Card> InTrace(AbilitySelectorTraceContext trace)
    {
        var cast = trace.Cast;
        if (trace.Discarded.Contains(cast.Source.ObjectId)) return [];
        var group = AttachedGroup(cast.World, cast.Source.Area.Host);
        if (trace.Discarded.Contains(group.Scheme.ObjectId)) return [];
        if (trace.EncounterStages is null)
            throw new RulesNotImplementedException("paired villain query requires projected encounter stages");
        var villain = AbilityPowerEncounterProjection.Current(group.Villain, cast, trace.EncounterStages);
        return villain is null || trace.Discarded.Contains(villain.ObjectId) ? [] : [villain];
    }

    private static EncounterDeckGroup AttachedGroup(World world, int host) =>
        Groups(world).SingleOrDefault(group => group.Scheme.ObjectId == host)
        ?? throw new RulesNotImplementedException("source is not attached to a paired encounter scheme");

    private static IReadOnlyList<EncounterDeckGroup> Groups(World world) =>
        world.EncounterTopology?.Groups
        ?? throw new RulesNotImplementedException("paired villain query requires encounter topology");
}
