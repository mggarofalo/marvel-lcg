using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Refreshes visible, explicitly-described relationships without creating inferred links.</summary>
internal sealed class BoardRelationshipOverlayController : IDisposable
{
    private readonly Main main;
    private readonly Control overlay = new()
    {
        Name = "RelationshipOverlay",
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ZIndex = 10,
    };
    private readonly List<Action> geometryUnsubscribers = [];
    private BoardRenderResult? board;
    private Action<IReadOnlyList<TableRelationshipDescriptor>>? interactionRelationshipsChanged;
    private bool layoutRefreshQueued;
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
        Unbind();
        board = next;
        snapshotRelationships = snapshot;
        promptRelationships = [];
        interactionRelationshipsChanged = relationships =>
        {
            if (ReferenceEquals(board, next))
            {
                promptRelationships = relationships;
                ScheduleRefresh();
            }
        };
        next.InteractionRelationshipsChanged += interactionRelationshipsChanged;
        ObserveGeometry(next);
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        if (layoutRefreshQueued)
        {
            return;
        }

        layoutRefreshQueued = true;
        Refresh();
        // The board's containers settle after the prompt rebuild. A second
        // deferred pass measures the final card rectangles, not a stale rail.
        Callable.From(RefreshAfterLayout).CallDeferred();
    }

    private void RefreshAfterLayout() => Callable.From(() =>
    {
        layoutRefreshQueued = false;
        Refresh();
    }).CallDeferred();

    private void Refresh()
    {
        if (board is null || board.IsCurrent?.Invoke() != true)
        {
            Present([]);
            return;
        }
        Present(Paths());
    }

    private List<Vector2[]> Paths()
    {
        var paths = new List<Vector2[]>();
        Rect2 viewport = overlay.GetGlobalRect();
        BoardRenderResult current = board ?? throw new InvalidOperationException(
            "a relationship path requires a rendered board");
        IReadOnlyList<Rect2> obstacles = current.VisibleCardControls()
            .Where(control => control.IsVisibleInTree())
            .Select(control => RelationshipOverlayVisibility.VisibleBounds(control, viewport))
            .OfType<Rect2>()
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
        Rect2 sourceRect = RelationshipOverlayVisibility.VisualBounds(source);
        Rect2 targetRect = RelationshipOverlayVisibility.VisualBounds(target);
        if (!RelationshipOverlayVisibility.EndpointIsVisible(source, viewport)
            || !RelationshipOverlayVisibility.EndpointIsVisible(target, viewport))
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

    private void ObserveGeometry(BoardRenderResult current)
    {
        var observed = new HashSet<Control>();
        foreach (CardControl card in current.VisibleCardControls())
        {
            for (Node? node = card; node is Control control; node = node.GetParent())
            {
                if (observed.Add(control))
                {
                    Observe(control);
                }
            }
        }
    }

    private void Observe(Control control)
    {
        Action moved = ScheduleRefresh;
        control.ItemRectChanged += moved;
        geometryUnsubscribers.Add(() =>
        {
            if (GodotObject.IsInstanceValid(control))
            {
                control.ItemRectChanged -= moved;
            }
        });

        if (control is not ScrollContainer scroll)
        {
            return;
        }

        ObserveScrollBar(scroll.GetHScrollBar());
        ObserveScrollBar(scroll.GetVScrollBar());
    }

    private void ObserveScrollBar(global::Godot.Range scrollBar)
    {
        global::Godot.Range.ValueChangedEventHandler scrolled = _ => ScheduleRefresh();
        scrollBar.ValueChanged += scrolled;
        geometryUnsubscribers.Add(() =>
        {
            if (GodotObject.IsInstanceValid(scrollBar))
            {
                scrollBar.ValueChanged -= scrolled;
            }
        });
    }

    private void Unbind()
    {
        if (board is not null && interactionRelationshipsChanged is not null)
        {
            board.InteractionRelationshipsChanged -= interactionRelationshipsChanged;
        }

        interactionRelationshipsChanged = null;
        foreach (Action unsubscribe in geometryUnsubscribers)
        {
            unsubscribe();
        }
        geometryUnsubscribers.Clear();
    }

    public void Dispose()
    {
        Unbind();
        overlay.GetParent()?.RemoveChild(overlay);
        overlay.QueueFree();
    }
}
