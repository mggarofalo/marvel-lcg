using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves other villains' paired schemes relative to an attachment host.</summary>
internal static class AbilityOtherVillainSchemeQuery
{
    internal static IReadOnlyList<Card> Candidates(World world) =>
        Groups(world).Select(group => group.Scheme).ToArray();

    internal static IReadOnlyList<Card> Live(AbilityQueryContext context)
    {
        if (context.SourceReference is not { } source || !DeckTypes.IsInPlay(source.Area.Type)) return [];
        return Select(context.World, source.Area.Host, group => group.CurrentVillain,
            scheme => scheme.Area.Type == DeckType.SideSchemesArea);
    }

    internal static IReadOnlyList<Card> InAreas(AbilityAdmissionContext context, AreaProjectionState state)
    {
        if (!state.SourceReferenceCurrent || state.Departed.Contains(context.Source.ObjectId)) return [];
        int host = state.Hosts.GetValueOrDefault(context.Source.ObjectId, context.Source.Area.Host);
        return Select(context.World, host,
            group => AbilityAreaEncounterProjection.Current(context.World, state, group.Villain),
            scheme => !state.Departed.Contains(scheme.ObjectId)
                && (scheme.Area.Type == DeckType.SideSchemesArea || state.Entered.Contains(scheme.ObjectId)));
    }

    internal static IReadOnlyList<Card> InTrace(AbilitySelectorTraceContext trace)
    {
        var cast = trace.Cast;
        if (trace.Discarded.Contains(cast.Source.ObjectId)) return [];
        if (trace.EncounterStages is null)
            throw new RulesNotImplementedException("other villain schemes require projected encounter stages");
        return Select(cast.World, cast.Source.Area.Host,
            group => trace.EncounterStages.TryGetValue(group.Villain.ObjectId, out int id)
                ? id < 0 || trace.Discarded.Contains(id) ? null : cast.World.Cards[id]
                : throw new RulesNotImplementedException("other villain schemes are missing a projected group"),
            scheme => scheme.Area.Type == DeckType.SideSchemesArea && !trace.Discarded.Contains(scheme.ObjectId));
    }

    private static Card[] Select(World world, int host, Func<EncounterDeckGroup, Card?> current,
        Func<Card, bool> schemeInPlay)
    {
        var groups = Groups(world);
        var owner = groups.SingleOrDefault(group => group.Stages.Any(stage => stage.ObjectId == host))
            ?? throw new RulesNotImplementedException("source is not attached to a paired encounter villain");
        if (current(owner)?.ObjectId != host) return [];
        // 07007: "place 1 threat on each other villain's side scheme."
        // Pair ownership is immutable; the active counter does not define "other".
        var others = groups.Where(group => group != owner && current(group) is not null).ToArray();
        if (others.Any(group => !schemeInPlay(group.Scheme)))
            throw new RulesNotImplementedException("a living other villain has no corresponding side scheme");
        return others.Select(group => group.Scheme).ToArray();
    }

    private static IReadOnlyList<EncounterDeckGroup> Groups(World world) =>
        world.EncounterTopology?.Groups
        ?? throw new RulesNotImplementedException("other villain schemes require encounter topology");
}
