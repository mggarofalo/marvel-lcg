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
                ? order.Select(id => world.Cards[id]) : area.Cards.Where(card => !order.Contains(card.ObjectId)))
            .Where(card => included.Contains(card.ObjectId));
    }
}
