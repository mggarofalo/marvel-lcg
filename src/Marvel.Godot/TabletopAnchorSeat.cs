using Marvel.View;

namespace Marvel.Godot;

/// <summary>Finds the public player workspace that contains a visible event or prompt anchor.</summary>
internal static class TabletopAnchorSeat
{
    internal static int? For(BoardPresentation? board, IReadOnlyList<int> anchors)
    {
        ArgumentNullException.ThrowIfNull(anchors);
        return board?.Areas
            .Where(area => area.Seat >= 0 && area.Cards.Concat(area.Removed)
                .Any(card => card.TargetId is { } id && anchors.Contains(id)))
            .Select(area => (int?)area.Seat)
            .FirstOrDefault();
    }
}
