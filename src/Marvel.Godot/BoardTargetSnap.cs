using Godot;

namespace Marvel.Godot;

/// <summary>Resolves a forgiving drop edge only among current engine-offered destinations.</summary>
internal sealed class BoardTargetSnap
{
    private int? previous;
    internal void Clear() => previous = null;

    internal int? At(BoardRenderResult board, CardPointerGesture gesture, IEnumerable<int> offered)
    {
        int? direct = board.CardAt(gesture.Position, gesture.Source);
        int[] ids = [.. offered.Distinct()];
        if (direct is not null)
        {
            previous = ids.Contains(direct.Value) ? direct : null;
            return direct;
        }
        var candidates = ids.Select(id => (Id: id, Control: board.ControlFor(id)))
            .Where(item => item.Control is not null && item.Control != gesture.Source
                && item.Control.IsVisibleInTree()).ToArray();
        Rect2[] bounds = [.. candidates.Select(item => SpatialCardFootprint.Face(item.Control!))];
        int held = Array.FindIndex(candidates, item => item.Id == previous);
        int? selected = TargetSnapGeometry.Select(bounds, gesture.Position, held);
        if (selected is { } index)
        {
            Control card = candidates[index].Control!;
            Vector2 inside = gesture.Position.Clamp(bounds[index].Position + Vector2.One,
                bounds[index].End - Vector2.One);
            if (CardPointerHitTest.Contains(card, inside)) return previous = candidates[index].Id;
        }
        previous = null;
        return null;
    }
}
