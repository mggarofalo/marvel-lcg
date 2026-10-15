using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Every installed controlled source has an independent, directly actionable tile.</summary>
internal static class SourceTableauRenderer
{
    internal static Control Create(BoardCardPresentation[] cards, Rect2 bounds,
        BoardRenderResult result, InterfaceScale scale, int seat, IReadOnlyDictionary<int, string>? names = null)
    {
        cards = SourcePresentationOrder.Arrange(seat, cards);
        IReadOnlyDictionary<int, string> copies = SourceCopyIdentity.Describe(seat, cards);
        var panel = new VBoxContainer { Name = "SourceTableau", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeConstantOverride("separation", 4);
        var scroll = new ScrollContainer { Name = "SourceTableauViewport",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto, FollowFocus = true };
        panel.AddChild(scroll);
        var layout = new SourceTableauLayout(bounds.Size.X, bounds.Size.Y, cards.Length);
        BoardCardPresentation?[] slots = SourcePresentationOrder.Slots(seat, cards, layout.Capacity);
        var grid = new GridContainer { Name = "SourceTableauGrid", Columns = layout.Columns,
            CustomMinimumSize = new Vector2(bounds.Size.X, layout.ContentHeight(slots.Length)),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", (int)SourceTableauLayout.Gap);
        grid.AddThemeConstantOverride("v_separation", (int)SourceTableauLayout.Gap);
        scroll.AddChild(grid);
        var gestures = new SourceGridGesture { Name = "SourceGridGesture" };
        panel.AddChild(gestures);
        gestures.Configure(grid, scroll, seat, result);
        result.Inspector.Register(cards);
        for (int index = 0; index < slots.Length; index++)
        {
            if (slots[index] is not { } card)
            {
                var vacancy = new Control { Name = $"EmptySourceSlot{index}",
                    CustomMinimumSize = new Vector2(layout.TileWidth, SourceTableauLayout.TileHeight),
                    MouseFilter = Control.MouseFilterEnum.Ignore };
                grid.AddChild(vacancy);
                gestures.TrackEmpty(vacancy);
                continue;
            }
            CardControl control = SourceTableauTile.CreateCard(card, layout.TileWidth, scale,
                card.TargetId is { } copyId ? copies[copyId] : "");
            control.Name = $"ProceduralCard{card.TargetId}";
            control.Position = layout.Position(index);
            AddHostRelation(control, card, names);
            var slot = new Control { Name = $"SourceSlot{card.TargetId}", CustomMinimumSize = control.CustomMinimumSize,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            grid.AddChild(slot);
            control.Position = Vector2.Zero;
            slot.AddChild(control);
            BindSlot(control, slot);
            gestures.Track(control, slot);
            grid.SortChildren += () => Callable.From(() =>
            { if (InteractionControl.IsUsable(control) && !control.TopLevel) result.UpdateRestingPose(control); }).CallDeferred();
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
            result.TrackPreviewOnly((Control)control.FindChild("SourceReorderGrip", true, false), card);
        }
        SourceTableauViewport.Bind(scroll, grid);
        return panel;
    }

    private static void AddHostRelation(CardControl control, BoardCardPresentation card,
        IReadOnlyDictionary<int, string>? names)
    {
        if (card.Persistent?.Relation.HostId is not { } host || names?.TryGetValue(host, out string? hostName) != true) return;
        var relation = CardSourceStrip.Label($"On {hostName}", "SourceHost", 9, CardTypography.Body);
        relation.AutowrapMode = TextServer.AutowrapMode.Off;
        relation.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        relation.AccessibilityName = $"Attached to {hostName}";
        control.FindChild("SourceSymbols", true, false).AddChild(relation);
    }

    private static void BindSlot(CardControl card, Control slot)
    {
        bool pending = false;
        void Schedule()
        {
            if (pending) return;
            pending = true;
            Callable.From(() => { pending = false; FitSlot(card, slot); }).CallDeferred();
        }
        card.MinimumSizeChanged += Schedule;
        card.Ready += Schedule;
    }

    private static void FitSlot(CardControl card, Control slot)
    {
        if (!InteractionControl.IsUsable(card) || !InteractionControl.IsUsable(slot)) return;
        Vector2 minimum = new(card.CustomMinimumSize.X, card.GetCombinedMinimumSize().Y);
        card.Size = minimum;
        slot.CustomMinimumSize = minimum;
    }
}
