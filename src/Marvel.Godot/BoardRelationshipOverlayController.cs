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
    private bool disposed;
    private bool layoutRefreshQueued;
    private IReadOnlyList<TableRelationshipDescriptor> promptRelationships = [];

    internal BoardRelationshipOverlayController(Main main)
    {
        this.main = main;
        main.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    internal void Bind(BoardRenderResult next)
    {
        Unbind();
        board = next;
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
        if (disposed || layoutRefreshQueued)
        {
            return;
        }

        layoutRefreshQueued = true;
        Refresh();
        // The board's containers settle after the prompt rebuild. A second
        // The deferred pass measures the final spatial card rectangles.
        Callable.From(RefreshAfterLayout).CallDeferred();
    }

    private void RefreshAfterLayout()
    {
        if (disposed)
        {
            return;
        }
        Callable.From(() =>
        {
            if (disposed)
            {
                return;
            }
            // Keep the coalescing guard through the settled render. Adding or
            // removing lines may notify an observed ancestor's layout, but it
            // cannot change the card geometry this pass has just measured.
            Refresh();
            layoutRefreshQueued = false;
        }).CallDeferred();
    }

    private void Refresh()
    {
        if (disposed)
        {
            return;
        }
        if (board is null || board.IsCurrent?.Invoke() != true)
        {
            Present([]);
            return;
        }
        Present(Paths());
    }

    private List<RelationshipPath> Paths()
    {
        var paths = new List<RelationshipPath>();
        Rect2 viewport = overlay.GetGlobalRect();
        BoardRenderResult current = board ?? throw new InvalidOperationException(
            "a relationship path requires a rendered board");
        foreach (TableRelationshipDescriptor relationship in promptRelationships)
        {
            if (PathFor(relationship, current.VisibleCardControls(), viewport) is { } path)
            {
                paths.Add(new RelationshipPath(relationship.Kind, path));
            }
        }
        return paths;
    }

    private Vector2[]? PathFor(
        TableRelationshipDescriptor relationship,
        IReadOnlyList<CardControl> cards,
        Rect2 viewport)
    {
        if (!TryEndpoints(relationship, out Control? source, out Control? target))
        {
            return null;
        }
        if (!RelationshipOverlayVisibility.EndpointIsVisible(source!, viewport)
            || !RelationshipOverlayVisibility.EndpointIsVisible(target!, viewport))
        {
            return null;
        }
        Rect2 sourceRect = RelationshipOverlayVisibility.VisualBounds(source!);
        Rect2 targetRect = RelationshipOverlayVisibility.VisualBounds(target!);
        Vector2[]? path = RelationshipRoutePlanner.Route(
            sourceRect, targetRect, Blockers(cards, source!, target!, viewport, sourceRect, targetRect));
        return path is null ? null : [.. path.Select(point => point - viewport.Position)];
    }

    private bool TryEndpoints(
        TableRelationshipDescriptor relationship, out Control? source, out Control? target)
    {
        source = board?.ControlFor(relationship.Subject);
        target = relationship.Related is { } related ? board?.ControlFor(related) : null;
        return source is not null && target is not null;
    }

    private static Rect2[] Blockers(
        IReadOnlyList<CardControl> cards,
        Control source,
        Control target,
        Rect2 viewport,
        Rect2 sourceRect,
        Rect2 targetRect) => [.. cards
            .Where(card => !ReferenceEquals(card, source) && !ReferenceEquals(card, target))
            .Where(control => control.IsVisibleInTree())
            .Select(control => RelationshipOverlayVisibility.VisibleBounds(control, viewport))
            .OfType<Rect2>()
            // Hand cards deliberately overlap. A neighbour touching an
            // endpoint must not trap the connector inside the fan; farther
            // unrelated cards still participate in route avoidance.
            .Where(bounds => !bounds.Intersects(sourceRect)
                && !bounds.Intersects(targetRect))];

    private void Present(IReadOnlyList<RelationshipPath> paths)
    {
        foreach (Node child in overlay.GetChildren())
        {
            overlay.RemoveChild(child);
            child.QueueFree();
        }
        foreach (RelationshipPath path in paths)
        {
            var line = new Line2D
            {
                Name = $"{path.Kind}Connector",
                Points = path.Points,
                Width = path.Kind == RelationshipKind.OfferedTarget ? 3 : 2,
                DefaultColor = ConnectorColor(path.Kind),
            };
            line.SetMeta("relationship_kind", path.Kind.ToString());
            overlay.AddChild(line);
        }
    }

    private static Color ConnectorColor(RelationshipKind kind) => ClientTheme.ToGodot(kind switch
    {
        RelationshipKind.OfferedGenerator => VisualSystem.Palette.Accent,
        RelationshipKind.Result => VisualSystem.Palette.Danger,
        RelationshipKind.Attachment => VisualSystem.Palette.Outline,
        _ => VisualSystem.Palette.Legal,
    });

    private void ObserveGeometry(BoardRenderResult current)
    {
        var observed = new HashSet<Control>();
        foreach (CardControl card in current.VisibleCardControls())
        {
            ObserveOnce(card, observed);
            for (Node? node = card.GetParent(); node is not null; node = node.GetParent())
            {
                if (node is ScrollContainer scroll)
                {
                    ObserveOnce(scroll, observed);
                }
            }
        }
    }

    private void ObserveOnce(Control control, HashSet<Control> observed)
    {
        if (observed.Add(control))
        {
            Observe(control);
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
        disposed = true;
        layoutRefreshQueued = false;
        Unbind();
        if (GodotObject.IsInstanceValid(overlay))
        {
            overlay.GetParent()?.RemoveChild(overlay);
            overlay.QueueFree();
        }
    }

    private sealed record RelationshipPath(RelationshipKind Kind, Vector2[] Points);
}
