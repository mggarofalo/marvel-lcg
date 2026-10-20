using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Ranks immutable villain/scheme pairs using only the requested projected board.</summary>
internal static class AbilityEncounterSchemeQueries
{
    internal static bool IsRanked(AbilityCardQuery query) => query is
        AbilityCardQuery.VillainsWithLeastSchemeThreat or AbilityCardQuery.VillainsWithMostSchemeThreat;

    internal static bool UsesGroupOrder(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.Query query => IsRanked(query.Kind),
        AbilityCardSelection.WithTrait filter => UsesGroupOrder(filter.Cards),
        AbilityCardSelection.FaceDown filter => UsesGroupOrder(filter.Cards),
        AbilityCardSelection.Last filter => UsesGroupOrder(filter.Cards),
        AbilityCardSelection.InObjectIdOrder filter => UsesGroupOrder(filter.Cards),
        AbilityCardSelection.WithMatchingPlayerArea filter => UsesGroupOrder(filter.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filter => UsesGroupOrder(filter.Cards),
        AbilityCardSelection.Discardable filter => UsesGroupOrder(filter.Cards),
        AbilityCardSelection.Ranked filter => UsesGroupOrder(filter.Cards),
        _ => false,
    };

    internal static IReadOnlyList<Card> InTrace(AbilityCardQuery query, AbilitySelectorTraceContext trace)
    {
        if (trace.EncounterStages is null || trace.SchemeThreat is null)
            throw new RulesNotImplementedException("ranked encounter schemes require projected stages and threat");
        var world = trace.Cast.World;
        return Rank(query, world,
            group => trace.EncounterStages.TryGetValue(group.Villain.ObjectId, out int id)
                ? id < 0 || trace.Discarded.Contains(id) ? null : world.Cards[id]
                : throw new RulesNotImplementedException("ranked encounter scheme is missing its projected group"),
            scheme => trace.SchemeThreat.GetValueOrDefault(scheme.ObjectId,
                scheme.Tokens.GetValueOrDefault("k_threat")),
            scheme => scheme.Area.Type == DeckType.SideSchemesArea
                && !trace.Discarded.Contains(scheme.ObjectId));
    }

    internal static IReadOnlyList<Card> InAreas(
        AbilityCardQuery query, World world, AreaProjectionState state) =>
        Rank(query, world,
            group => AbilityAreaEncounterProjection.Current(world, state, group.Villain),
            state.ThreatOf,
            scheme => !state.Departed.Contains(scheme.ObjectId)
                && (scheme.Area.Type == DeckType.SideSchemesArea || state.Entered.Contains(scheme.ObjectId)));

    private static Card[] Rank(
        AbilityCardQuery query, World world, Func<EncounterDeckGroup, Card?> current,
        Func<Card, long> threat, Func<Card, bool> schemeInPlay)
    {
        if (!IsRanked(query)) throw new InvalidOperationException("Unknown ranked encounter query");
        var groups = world.EncounterTopology?.Groups
            ?? throw new RulesNotImplementedException("ranked encounter schemes require encounter topology");
        var living = groups.Select(group => (Group: group, Villain: current(group)))
            .Where(pair => pair.Villain is not null).ToArray();
        if (living.Any(pair => !schemeInPlay(pair.Group.Scheme)))
            throw new RulesNotImplementedException("a projected living villain has no corresponding scheme");
        if (living.Length == 0) return [];
        long extreme = query == AbilityCardQuery.VillainsWithMostSchemeThreat
            ? living.Max(pair => threat(pair.Group.Scheme))
            : living.Min(pair => threat(pair.Group.Scheme));
        return living.Where(pair => threat(pair.Group.Scheme) == extreme)
            .Select(pair => pair.Villain!).ToArray();
    }
}
