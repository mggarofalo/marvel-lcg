using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves board relations whose projected membership replaces live selection.</summary>
internal static class AbilityProjectedRelationQueries
{
    internal static List<Card>? Select(
        AbilityCardQuery query, AreaProjectionState state, AbilityAdmissionContext context) => query switch
    {
        AbilityCardQuery.VillainAndMinionsEngagedWithYou =>
            AbilityProjectedDamageRecipients.Select(state, context),
        AbilityCardQuery.OtherVillainSideSchemes =>
            [.. AbilityOtherVillainSchemeQuery.InAreas(context, state)],
        AbilityCardQuery.VillainOfAttachedScheme =>
            [.. AbilityAttachedSchemeVillainQuery.InAreas(context, state)],
        AbilityCardQuery.ActiveVillainSideScheme =>
            [.. AbilityActiveSchemeQuery.InAreas(context.World, state)],
        _ when AbilityEncounterSchemeQueries.IsRanked(query) =>
            [.. AbilityEncounterSchemeQueries.InAreas(query, context.World, state)],
        _ => null,
    };
}
