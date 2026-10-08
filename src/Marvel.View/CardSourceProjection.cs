using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>One visibility policy for live and historical effect-source references.</summary>
internal static class CardSourceProjection
{
    internal static CardValueSourceDescriptor? Describe(
        World world, CardSourceSnapshot origin, IReadOnlyDictionary<int, CardDescriptor> readable, ViewScope scope)
    {
        CardSourceExposure exposure = origin.Exposure;
        if (!Authorized(exposure, scope)) return null;
        bool live = IsCurrentSource(world, origin, readable);
        // Historical origins have no current target ID. A visible recycled
        // physical card never becomes a link to the earlier source copy.
        return new(origin.FaceId, origin.Title, live ? origin.CardId : null, !live)
        {
            RulesText = exposure.ReplacementIdentity ? string.Empty : world.Facts.Text(origin.FaceId),
            RulesMarkup = exposure.ReplacementIdentity ? string.Empty : world.Facts.FormattedText(origin.FaceId),
        };
    }

    private static bool Authorized(CardSourceExposure exposure, ViewScope scope) =>
        exposure.ReplacementIdentity
        || (exposure.Zone == DeckType.HandsArea && exposure.Player >= 0
            ? scope.Includes(exposure.Player)
            : exposure.FaceUp);

    private static bool IsCurrentSource(
        World world, CardSourceSnapshot origin, IReadOnlyDictionary<int, CardDescriptor> readable)
    {
        Card actual = world.Cards[origin.CardId];
        return DeckTypes.IsInPlay(actual.Area.Type)
            && actual.Incarnation == origin.Incarnation
            && EffectiveCards.FaceId(actual) == origin.FaceId
            && readable.TryGetValue(origin.CardId, out CardDescriptor? visible)
            && visible.Face?.Id == origin.FaceId;
    }

}
