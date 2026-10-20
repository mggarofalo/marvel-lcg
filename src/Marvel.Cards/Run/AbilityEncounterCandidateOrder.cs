using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Applies projected villain-area chronology to candidates in board order.</summary>
internal static class AbilityEncounterCandidateOrder
{
    internal static IEnumerable<Card> Apply(IEnumerable<Card> candidates, World world, int[]? order)
    {
        if (order is null) return candidates;
        var included = candidates.Select(card => card.ObjectId).ToHashSet();
        // Engine choice: a promoted stage is appended to its villain area, matching MoveCard.
        return world.Areas.SelectMany(area => area.Type == DeckType.VillainArea
                ? order.Where(id => StageArea(world, id) == area.Id).Select(id => world.Cards[id]) : area.Cards.Where(card => !order.Contains(card.ObjectId)))
            .Where(card => included.Contains(card.ObjectId));
    }
    private static int StageArea(World world, int id)
    {
        var card = world.Cards[id];
        return world.EncounterTopology!.Groups.SingleOrDefault(group => group.Stages.Contains(card))
            ?.CurrentVillain?.Area.Id ?? card.Area.Id;
    }

}
