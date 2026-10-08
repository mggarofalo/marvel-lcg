using Godot;

namespace Marvel.Godot;

/// <summary>Keeps upright captions and source controls alongside their physical face.</summary>
internal static class SpatialCardSidecar
{
    internal static VBoxContainer For(CardControl card)
    {
        VBoxContainer? existing = card.GetNodeOrNull<VBoxContainer>("SpatialOverlay/SpatialControls/Contents");
        if (existing is not null) return existing;
        // PanelContainer places its direct children inside the face. The free
        // overlay owns the external position; its scroll bounds cannot enlarge
        // the physical face when captions or several actions need more room.
        var overlay = new Control
        {
            Name = "SpatialOverlay", MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var scroll = new ScrollContainer
        {
            Name = "SpatialControls", ClipContents = true,
            FollowFocus = true,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
            MouseFilter = Control.MouseFilterEnum.Pass,
        };
        var sidecar = new VBoxContainer
        {
            Name = "Contents", MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(sidecar);
        overlay.AddChild(scroll);
        card.AddChild(overlay);
        Callable.From(() => Place(card)).CallDeferred();
        return sidecar;
    }

    internal static void State(CardControl card, Marvel.View.BoardCardPresentation presentation)
    {
        For(card);
        var state = CardLiveStateRendering.Create(presentation, SpatialCardFootprint.SidecarWidth, compact: true);
        if (SpatialTableZones.IsExhausted(presentation))
        {
            Label title = CardLiveStateRendering.AddLabel(state, presentation.Title, "UprightIdentity", 11);
            state.MoveChild(title, 0);
        }
        card.GetNode<Control>("SpatialOverlay").AddChild(state);
        state.MinimumSizeChanged += () => Callable.From(() => Place(card)).CallDeferred();
        Callable.From(() => Place(card)).CallDeferred();
    }

    internal static void Place(CardControl card)
    {
        if (InteractionControl.IsUsable(card)
            && card.GetNodeOrNull<ScrollContainer>("SpatialOverlay/SpatialControls") is { } scroll)
        {
            SpatialCardFootprint.PlaceSidecar(card, scroll);
            if (card.GetNodeOrNull<VBoxContainer>("SpatialOverlay/LiveState") is { } state)
            {
                SpatialCardFootprint.PlaceSidecar(card, state);
                float height = state.GetCombinedMinimumSize().Y;
                state.Size = new Vector2(SpatialCardFootprint.SidecarWidth, height);
                Vector2 offset = new(0, height > 0 ? height + 4 : 0);
                scroll.Position += offset.Rotated(-card.Rotation);
                scroll.Size = new Vector2(scroll.Size.X, Math.Max(0, scroll.Size.Y - offset.Y));
            }
        }
    }

    internal static void Caption(CardControl card, string text)
    {
        VBoxContainer sidecar = For(card);
        sidecar.AddChild(new Label
        {
            Name = "PhysicalCardCaption", Text = text,
            ThemeTypeVariation = GodotThemeVariations.Caption,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
    }
}
