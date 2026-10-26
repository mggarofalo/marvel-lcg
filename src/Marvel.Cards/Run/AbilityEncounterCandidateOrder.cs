using Marvel.Rules.State;
using Marvel.Cards.Dsl;

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
            // Selector modifier reads may materialize empty areas; the ordering
            // is a snapshot of the candidate board, taken before those reads.
            .Where(card => included.Contains(card.ObjectId)).ToArray();
    }

    internal static IEnumerable<Card> ApplySelectorOrder(IEnumerable<Card> candidates,
        AbilityCardSelection selector, AbilitySelectorTraceContext trace)
    {
        if (AbilitySelectorTrace.UsesObjectIdOrder(selector))
            return candidates.OrderBy(card => card.ObjectId);
        // Engine choice: the simultaneous villain/engaged-minion cohort puts its
        // villain first, independently of projected villain-area chronology.
        return VillainFirst(selector, trace)
            ? candidates.OrderByDescending(card => CardKinds.IsVillain(EffectiveCards.Kind(card, trace.Cast.World.Facts)))
            : candidates;
    }

    private static bool VillainFirst(AbilityCardSelection? selector,
        AbilitySelectorTraceContext trace) => selector switch
    {
        AbilityCardSelection.Query { Kind: AbilityCardQuery.VillainAndMinionsEngagedWithYou } => true,
        AbilityCardSelection.WithTrait filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.WithStatus filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.FaceDown filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.Last filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.WithMatchingPlayerArea filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.WithoutAnotherCopyAttached filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.Discardable filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.Ranked filter => VillainFirst(filter.Cards, trace),
        AbilityCardSelection.FirstAvailable fallback => VillainFirst(fallback.Options.FirstOrDefault(
            option => AbilitySelectorTrace.MatchingCandidates(option, trace).Any()), trace),
        _ => false,
    };

    private static int StageArea(World world, int id)
    {
        var card = world.Cards[id];
        return world.EncounterTopology!.Groups.SingleOrDefault(group => group.Stages.Contains(card))
            ?.CurrentVillain?.Area.Id ?? card.Area.Id;
    }

}
