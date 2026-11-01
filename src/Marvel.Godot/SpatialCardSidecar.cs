using Godot;

namespace Marvel.Godot;

/// <summary>Keeps upright live state and local controls within the physical card's column.</summary>
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
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
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
        sidecar.MinimumSizeChanged += () => Callable.From(() => Place(card)).CallDeferred();
        Callable.From(() => Place(card)).CallDeferred();
        return sidecar;
    }

    internal static void State(CardControl card, Marvel.View.BoardCardPresentation presentation)
    {
        var scale = card.HasMeta("card_reading_scale") ? (InterfaceScale)card.GetMeta("card_reading_scale").AsInt32() : InterfaceScale.Percent100;
        var state = CardLiveStateRendering.Create(presentation, Math.Min(card.Size.X, card.Size.Y) - 12, compact: true, scale: scale);
        if (state.GetChildCount() == 0) { state.QueueFree(); return; }
        For(card);
        if (SpatialTableZones.IsExhausted(presentation))
        {
            Label title = CardLiveStateRendering.AddLabel(state, presentation.Title, "UprightIdentity", 11);
            state.MoveChild(title, 0);
        }
        var panel = new PanelContainer { Name = "LiveStatePanel", MouseFilter = Control.MouseFilterEnum.Ignore };
        using var style = new StyleBoxFlat { BgColor = CardFaceStyle.Ink,
            ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 3, ContentMarginBottom = 3 };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(state);
        card.GetNode<Control>("SpatialOverlay").AddChild(panel);
        state.MinimumSizeChanged += () => Callable.From(() => Place(card)).CallDeferred();
        Callable.From(() => Place(card)).CallDeferred();
    }

    internal static void Place(CardControl card)
    {
        if (InteractionControl.IsUsable(card)
            && card.GetNodeOrNull<ScrollContainer>("SpatialOverlay/SpatialControls") is { } scroll)
        {
            SpatialCardFootprint.PlaceSidecar(card, scroll);
            if (scroll.FindChild("SourceCollection", true, false) is Control)
            {
                scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
                scroll.Size = new Vector2(scroll.Size.X, scroll.GetChild<Control>(0).GetCombinedMinimumSize().Y);
            }
            if (card.GetNodeOrNull<PanelContainer>("SpatialOverlay/LiveStatePanel") is { } state)
            {
                float height = state.GetCombinedMinimumSize().Y;
                Rect2 bounds = SpatialCardFootprint.Face(card);
                state.Rotation = -card.Rotation;
                state.Position = ((Control)state.GetParent()).GetGlobalTransform().AffineInverse()
                    * new Vector2(bounds.Position.X + 3, bounds.End.Y - height * card.Scale.Y - 3);
                state.Size = new Vector2(bounds.Size.X / card.Scale.X - 6, height);
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
