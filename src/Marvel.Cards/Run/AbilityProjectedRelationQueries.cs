using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves board relations whose projected membership replaces live selection.</summary>
internal static class AbilityProjectedRelationQueries
{
    internal static List<Card>? Select(
        AbilityCardQuery query, AreaProjectionState state, AbilityAdmissionContext context) => query switch
    {
        AbilityCardQuery.UpgradesOnThis => UpgradesOnSource(state, context),
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
    private static List<Card> UpgradesOnSource(AreaProjectionState state, AbilityAdmissionContext context)
    {
        if (!state.SourceReferenceCurrent || context.Query.SourceReference is not { } source
            || state.Departed.Contains(source.ObjectId)) return [];
        return [.. context.World.Cards.Where(card =>
            !state.Departed.Contains(card.ObjectId)
            && (DeckTypes.IsInPlay(card.Area.Type) || state.Entered.Contains(card.ObjectId))
            && state.Hosts.GetValueOrDefault(card.ObjectId, card.Area.Host) == source.ObjectId
            && EffectiveCards.Kind(card, context.World.Facts) == CardKind.Upgrade)
            .OrderBy(card => card.ObjectId)];
    }
}
