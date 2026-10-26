using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;

namespace Marvel.Cards.Run;

/// <summary>One simultaneous cohort from the projected villain and resolving seat.</summary>
internal static class AbilityProjectedDamageRecipients
{
    internal static List<Card> Select(AreaProjectionState state, AbilityAdmissionContext context) =>
        [.. AbilityProjectedStateQueries.ProjectedEvery(new AbilityCardSelection.Query(AbilityCardQuery.Villain), state, context),
            .. AbilityProjectedStateQueries.ProjectedEvery(new AbilityCardSelection.Query(AbilityCardQuery.Minions), state, context)
                .Where(card => state.EngagedWith.GetValueOrDefault(card.ObjectId,
                    card.Area.Type == DeckType.EngagedEnemiesArea ? card.Area.PlayArea.Player : -1)
                    == Resolver(context))];
}
