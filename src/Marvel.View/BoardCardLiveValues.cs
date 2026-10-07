using System.Globalization;

namespace Marvel.View;

/// <summary>Copies authorized live face values without evaluating game effects.</summary>
internal static class BoardCardLiveValues
{
    internal static BoardCardPresentation Apply(BoardCardPresentation result, CardDescriptor card, bool inPlay) =>
        result with
        {
            Damage = card.Face!.Damage,
            Statuses = card.State?.Statuses ?? [],
            Retaliate = inPlay && card.Face.Fields.TryGetValue("retaliate", out long value) ? value : null,
            Counters = card.Face.Counters.OrderBy(counter => counter.Key, StringComparer.Ordinal)
                .Select(counter => new BoardFieldPresentation(
                    BoardCardPresentationFactory.Humanize(counter.Key, false).ToUpperInvariant(),
                    counter.Value.ToString(CultureInfo.InvariantCulture))).ToArray(),
        };
}
