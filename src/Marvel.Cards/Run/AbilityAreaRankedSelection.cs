using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;

namespace Marvel.Cards.Run;

/// <summary>Ranks the selected candidates on one projected board.</summary>
internal static class AbilityAreaRankedSelection
{
    internal static List<Card> Select(
        AbilityCardSelection.Ranked ranked, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        var among = ProjectedEvery(ranked.Cards, state, context).ToList();
        if (among.Count == 0)
        {
            return [];
        }
        long Rank(Card card) => ProjectedRank(ranked.By, card, state, context);
        long extreme = ranked.Maximum ? among.Max(Rank) : among.Min(Rank);
        return [.. among.Where(card => Rank(card) == extreme)];
    }

    private static long ProjectedRank(
        AbilityCardRank rank, Card card, AreaProjectionState state,
        AbilityAdmissionContext context) => rank switch
        {
            AbilityCardRank.Threat => state.ThreatOf(card),
            AbilityCardRank.Cost => context.World.Facts.PrintedValue(
                card.FaceId, "Cost", context.World.Players),
            AbilityCardRank.Attack => state.ModifiedOf(context, card, "attack"),
            AbilityCardRank.PrintedHealth => EffectiveCards.BaseValue(
                card, context.World.Facts, "HP", context.World.Players),
            AbilityCardRank.RemainingHealth => Math.Max(0, state.HealthOf(context, card) - state.DamageOf(card)),
            _ => throw new InvalidOperationException(
                "Unknown compiled card rank in area projection"),
        };

}
