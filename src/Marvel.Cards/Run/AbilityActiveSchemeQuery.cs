using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Follows the active counter to its physical paired scheme on one observed board.</summary>
internal static class AbilityActiveSchemeQuery
{
    internal static IReadOnlyList<Card> Candidates(World world) =>
        Groups(world).Select(group => group.Scheme).ToArray();

    internal static IReadOnlyList<Card> Live(World world)
    {
        _ = Groups(world);
        var villain = EncounterRouting.Villain(world);
        return villain is null ? [] : [EncounterSchemeRelations.Scheme(world, villain)];
    }

    internal static IReadOnlyList<Card> InAreas(World world, AreaProjectionState state)
    {
        var group = Active(world, state.ActiveVillain);
        if (group is null) return [];
        var current = AbilityAreaEncounterProjection.Current(world, state, group.Villain);
        if (current?.ObjectId != state.ActiveVillain || state.Departed.Contains(group.Scheme.ObjectId)
            || (group.Scheme.Area.Type != DeckType.SideSchemesArea && !state.Entered.Contains(group.Scheme.ObjectId)))
            throw new RulesNotImplementedException("projected active villain has no live paired scheme");
        return [group.Scheme];
    }

    internal static IReadOnlyList<Card> InTrace(AbilitySelectorTraceContext trace)
    {
        var group = Active(trace.Cast.World, trace.CurrentVillain);
        if (group is null) return [];
        if (trace.EncounterStages is null
            || !trace.EncounterStages.TryGetValue(group.Villain.ObjectId, out int current)
            || current != trace.CurrentVillain || trace.Discarded.Contains(current)
            || trace.Discarded.Contains(group.Scheme.ObjectId) || group.Scheme.Area.Type != DeckType.SideSchemesArea)
            throw new RulesNotImplementedException("projected active villain has no live paired scheme");
        return [group.Scheme];
    }

    private static EncounterDeckGroup? Active(World world, int active)
    {
        var groups = Groups(world);
        if (active < 0) return null;
        return groups.SingleOrDefault(group => group.Stages.Any(stage => stage.ObjectId == active))
            ?? throw new RulesNotImplementedException("active villain has no physical encounter group");
    }

    private static IReadOnlyList<EncounterDeckGroup> Groups(World world) =>
        world.EncounterTopology?.Groups
        ?? throw new RulesNotImplementedException("active villain side scheme requires encounter topology");
}
