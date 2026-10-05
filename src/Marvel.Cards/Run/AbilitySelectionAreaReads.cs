using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Explicit area reads belong to the compiled selection, including wrappers.
internal static class AbilitySelectionAreaReads
{
    internal static HashSet<DeckType> For(AbilityCardSelection selection)
    {
        var areas = new HashSet<DeckType>();
        Collect(selection, areas);
        return areas;
    }

    private static void Collect(AbilityCardSelection selection, HashSet<DeckType> areas)
    {
        if (Operand(selection) is { } child) Collect(child, areas);
        switch (selection)
        {
            case AbilityCardSelection.InAreas searched:
                areas.UnionWith(searched.Areas.Select(AbilitySelectorEvaluation.AreaType)); break;
            case AbilityCardSelection.InPlayerArea named: areas.Add(named.Area); break;
            case AbilityCardSelection.WithMatchingPlayerArea filtered:
                areas.Add(filtered.Area); break;
            case AbilityCardSelection.WithTrait or AbilityCardSelection.FaceDown
                or AbilityCardSelection.Last or AbilityCardSelection.InObjectIdOrder
                or AbilityCardSelection.WithoutAnotherCopyAttached or AbilityCardSelection.Discardable
                or AbilityCardSelection.Ranked:
            case AbilityCardSelection.DefeatedWithProfile or AbilityCardSelection.Bound
                or AbilityCardSelection.Query or AbilityCardSelection.Titled or AbilityCardSelection.EnemiesWithTrait: break;
            default: throw new InvalidOperationException("Unknown compiled selector in area-dependency analysis");
        }
    }

    private static AbilityCardSelection? Operand(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.WithMatchingPlayerArea filtered => filtered.Cards,
        AbilityCardSelection.WithTrait filtered => filtered.Cards,
        AbilityCardSelection.FaceDown filtered => filtered.Cards,
        AbilityCardSelection.Last filtered => filtered.Cards,
        AbilityCardSelection.InObjectIdOrder ordered => ordered.Cards,
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => filtered.Cards,
        AbilityCardSelection.Discardable filtered => filtered.Cards,
        AbilityCardSelection.Ranked ranked => ranked.Cards,
        _ => null,
    };
}
