using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Adds visible table placement and live state to one card face descriptor.</summary>
internal static class CardDescriptorProjection
{
    internal static CardDescriptor Filter(
        CardDescriptor card,
        ViewScope scope,
        HashSet<int> visible) => card.Audience.IsVisible(scope)
            ? card with
            {
                Host = visible.Contains(card.Host) ? card.Host : -1,
                Audience = CardAudience.Nobody,
                Addressable = false,
            }
            : card with
            {
                Id = card.Addressable ? card.Id : null,
                FaceUp = false,
                Ready = card.Addressable ? card.Ready : true,
                Host = card.Addressable && visible.Contains(card.Host) ? card.Host : -1,
                Face = null,
                Location = null,
                State = null,
                Audience = CardAudience.Nobody,
                Addressable = false,
            };

    internal static CardDescriptor WithTableState(
        World world,
        Card card,
        CardBack back,
        CardFaceDescriptor face,
        CardAudience audience,
        bool addressable) => new(
            card.ObjectId, back, card.FaceUp, card.Ready, card.Area.Host, face)
        {
            Audience = audience,
            Addressable = addressable,
            Location = new CardLocationDescriptor(
                card.Area.Id,
                card.Area.Type.ToString(),
                card.Area.PlayArea.Player,
                card.Area.Type == DeckType.EngagedEnemiesArea ? card.Area.PlayArea.Player : -1),
            State = new CardStateDescriptor(
                card.Ready,
                face.Damage,
                Threat(face.Fields),
                face.Counters,
                face.Fields,
                Statuses(world, card)),
        };

    private static long? Threat(IReadOnlyDictionary<string, long> fields) =>
        fields.TryGetValue("k_threat", out long threat) ? threat : null;

    private static IReadOnlyList<string> Statuses(World world, Card card) =>
        [.. world.Areas
            .Where(area => area.Type == DeckType.StatusArea && area.Host == card.ObjectId)
            .SelectMany(area => area.Cards)
            .Where(status => status.FaceUp)
            .Select(status => world.Facts.Title(status.FaceId))];
}
