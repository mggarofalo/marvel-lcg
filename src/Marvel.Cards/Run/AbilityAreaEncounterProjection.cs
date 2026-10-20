using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves immutable encounter groups against projected area membership.</summary>
internal static class AbilityAreaEncounterProjection
{
    internal static Card? Current(World world, AreaProjectionState state, Card card)
    {
        var group = Group(world, card);
        return group is null ? card : group.Stages.SingleOrDefault(stage =>
            !state.Departed.Contains(stage.ObjectId)
            && (stage.Area.Type == DeckType.VillainArea || state.Entered.Contains(stage.ObjectId)));
    }

    internal static Card? Next(World world, Card card)
    {
        var group = Group(world, card)
            ?? throw new RulesNotImplementedException("a projected villain has no encounter group");
        int index = group.Stages.ToList().IndexOf(card);
        return index + 1 < group.Stages.Count ? group.Stages[index + 1] : null;
    }

    internal static void Advance(
        AbilityAreaProjection projection, AreaProjectionState state, Card target, Card? next)
    {
        if (next is not null)
        {
            if (state.ActiveVillain == target.ObjectId) state.ActiveVillain = next.ObjectId;
            return;
        }
        var world = projection.context.World;
        var group = Group(world, target)!;
        // pack:mc03:new-rules.multiple-villains-and-encounter-decks:
        // "When a villain is defeated, their side scheme is also removed".
        projection.couldDiscard |= projection.HostedCardsChangeArea(state, group.Scheme.ObjectId);
        projection.MarkDiscardedTree(state, group.Scheme);
        if (state.ActiveVillain == target.ObjectId) SelectActive(world, state);
    }

    private static void SelectActive(World world, AreaProjectionState state)
    {
        var living = world.EncounterTopology!.Groups
            .Where(group => Current(world, state, group.Villain) is not null).ToArray();
        if (living.Length == 0)
        {
            state.ActiveVillain = -1;
            return;
        }
        long maximum = living.Max(group => state.ThreatOf(group.Scheme));
        var candidates = living.Where(group => state.ThreatOf(group.Scheme) == maximum).ToArray();
        // The insert assigns tied active-villain selection to the first player.
        if (candidates.Length != 1)
            throw new RulesNotImplementedException("projected active-villain defeat requires a tied-villain choice");
        state.ActiveVillain = Current(world, state, candidates[0].Villain)!.ObjectId;
    }

    private static EncounterDeckGroup? Group(World world, Card card) =>
        world.EncounterTopology?.Groups.SingleOrDefault(group => group.Stages.Contains(card));
}
