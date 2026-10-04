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

    internal static void Place(CardControl card)
    {
        if (InteractionControl.IsUsable(card)
            && card.GetNodeOrNull<ScrollContainer>("SpatialOverlay/SpatialControls") is { } scroll)
            SpatialCardFootprint.PlaceSidecar(card, scroll);
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
