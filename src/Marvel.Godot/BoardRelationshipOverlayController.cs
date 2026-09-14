using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Refreshes visible, explicitly-described relationships without creating inferred links.</summary>
internal sealed class BoardRelationshipOverlayController : IDisposable
{
    private readonly Main main;
    private readonly Control overlay = new()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ZIndex = 10,
    };
    private BoardRenderResult? board;
    private IReadOnlyList<TableRelationshipDescriptor> snapshotRelationships = [];
    private IReadOnlyList<TableRelationshipDescriptor> promptRelationships = [];

    internal BoardRelationshipOverlayController(Main main)
    {
        this.main = main;
        main.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    internal void Bind(BoardRenderResult next, IReadOnlyList<TableRelationshipDescriptor> snapshot)
    {
        board = next;
        snapshotRelationships = snapshot;
        promptRelationships = [];
        next.InteractionRelationshipsChanged += relationships =>
        {
            if (ReferenceEquals(board, next))
            {
                promptRelationships = relationships;
                ScheduleRefresh();
            }
        };
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        Refresh();
        // The board's containers settle after the prompt rebuild. A second
        // deferred pass measures the final card rectangles, not a stale rail.
        Callable.From(RefreshAfterLayout).CallDeferred();
    }

    private void RefreshAfterLayout() => Callable.From(Refresh).CallDeferred();

    private void Refresh()
    {
        if (board is null || board.IsCurrent?.Invoke() != true)
        {
            Present([]);
            return;
        }
        Present(Paths());
    }

    private IReadOnlyList<Vector2[]> Paths()
    {
        var paths = new List<Vector2[]>();
        Rect2 viewport = overlay.GetGlobalRect();
        IReadOnlyList<Rect2> obstacles = board.VisibleCardControls()
            .Where(control => control.IsVisibleInTree())
            .Select(control => control.GetGlobalRect())
            .Where(rect => rect.Intersects(viewport))
            .ToArray();
        foreach (TableRelationshipDescriptor relationship in snapshotRelationships.Concat(promptRelationships))
        {
            if (PathFor(relationship, obstacles, viewport) is { } path)
            {
                paths.Add(path);
            }
        }
        return paths;
    }

    private Vector2[]? PathFor(
        TableRelationshipDescriptor relationship,
        IReadOnlyList<Rect2> obstacles,
        Rect2 viewport)
    {
        if (relationship.Related is not { } related
            || board?.ControlFor(relationship.Subject) is not { } source
            || board.ControlFor(related) is not { } target)
        {
            return null;
        }
        Rect2 sourceRect = source.GetGlobalRect();
        Rect2 targetRect = target.GetGlobalRect();
        if (!viewport.HasPoint(sourceRect.GetCenter())
            || !viewport.HasPoint(targetRect.GetCenter()))
        {
            return null;
        }
        Rect2[] blockers = obstacles.Where(rect => rect != sourceRect && rect != targetRect).ToArray();
        Vector2[]? path = RelationshipRoutePlanner.Route(sourceRect, targetRect, blockers);
        return path is null ? null : [.. path.Select(point => point - viewport.Position)];
    }

    private void Present(IReadOnlyList<Vector2[]> paths)
    {
        foreach (Node child in overlay.GetChildren())
        {
            overlay.RemoveChild(child);
            child.QueueFree();
        }
        foreach (Vector2[] path in paths)
        {
            overlay.AddChild(new Line2D
            {
                Points = path,
                Width = 2,
                DefaultColor = ClientTheme.ToGodot(VisualSystem.Palette.Legal),
            });
        }
    }

    public void Dispose()
    {
        overlay.GetParent()?.RemoveChild(overlay);
        overlay.QueueFree();
    }
}
