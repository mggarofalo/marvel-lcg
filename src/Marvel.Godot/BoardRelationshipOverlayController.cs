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
                Refresh();
                Callable.From(Refresh).CallDeferred();
            }
        };
        Refresh();
        Callable.From(Refresh).CallDeferred();
    }

    private void Refresh()
    {
        if (board is null || board.IsCurrent?.Invoke() != true)
        {
            Present([]);
            return;
        }
        var paths = new List<Vector2[]>();
        IReadOnlyList<Rect2> obstacles = board.VisibleCardControls()
            .Select(control => control.GetGlobalRect()).ToArray();
        foreach (TableRelationshipDescriptor relationship in snapshotRelationships.Concat(promptRelationships))
        {
            if (relationship.Related is not { } related
                || board.ControlFor(relationship.Subject) is not { } source
                || board.ControlFor(related) is not { } target)
            {
                continue;
            }
            Rect2 sourceRect = source.GetGlobalRect();
            Rect2 targetRect = target.GetGlobalRect();
            Rect2[] blockers = obstacles.Where(rect => rect != sourceRect && rect != targetRect).ToArray();
            Vector2[]? path = RelationshipRoutePlanner.Route(sourceRect, targetRect, blockers);
            if (path is not null)
            {
                paths.Add(path);
            }
        }
        Present(paths);
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
