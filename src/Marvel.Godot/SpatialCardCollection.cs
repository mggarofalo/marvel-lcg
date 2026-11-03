using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Continuously scrollable physical cards with visible overflow and no object picker.</summary>
internal static class SpatialCardCollection
{
    internal static IReadOnlyList<CardControl> Render(Control surface, Rect2 bounds, string zone,
        BoardCardPresentation[] cards, BoardRenderResult result, SpatialCardFactory faces)
    {
        var panel = new VBoxContainer { Name = $"Collection{zone}", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeConstantOverride("separation", 4);
        surface.AddChild(panel);
        panel.AddChild(new Label { Text = $"{TabletopAreaNames.Region(zone)} · {cards.Length}",
            ThemeTypeVariation = GodotThemeVariations.Caption, MouseFilter = Control.MouseFilterEnum.Ignore });
        var scroll = new ScrollContainer { Name = $"Continuous{zone}", FollowFocus = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(row);
        result.Inspector.Register(cards);
        var controls = new List<CardControl>();
        foreach (BoardCardPresentation card in cards)
        {
            CardLayoutMetrics layout = CardControl.LayoutFor(card, CardDisplaySize.Board, faces.Scale);
            float factor = Math.Min(1, (bounds.Size.Y - 40 - 48) / (layout.MinimumHeight + 32));
            if (zone == "AlliesArea")
            {
                int visible = Math.Min(3, cards.Length);
                float slotWidth = (bounds.Size.X - (visible - 1) * 12) / visible;
                factor = Math.Min(factor, slotWidth / layout.Width);
            }
            CardControl control = faces.CreateFitted(card, factor);
            control.Name = $"ProceduralCard{card.TargetId}";
            SpatialCardSidecar.State(control, card);
            controls.Add(control);
            Vector2 occupied = control.CustomMinimumSize;
            var slot = new Control { CustomMinimumSize = occupied + new Vector2(0, 32 * factor + 48),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            row.AddChild(slot);
            slot.AddChild(control);
            control.Position = new Vector2(0, 32 * factor);
            if (SpatialTableZones.IsExhausted(card)) ExhaustedBadge.Add(control, faces.ReadingScale);
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
            row.SortChildren += () => Callable.From(() =>
            { if (InteractionControl.IsUsable(control)) result.UpdateRestingPose(control); }).CallDeferred();
        }
        // The cue counts the cards the row does not wholly show; scrolling reveals them.
        Label overflow = OverflowCue.Create("ContinuousOverflow", 0, "Scroll sideways for the rest.", faces.ReadingScale);
        overflow.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.AddChild(overflow);
        void MeasureOverflow() => Callable.From(() =>
        {
            if (!InteractionControl.IsUsable(scroll)) return;
            float left = scroll.ScrollHorizontal;
            OverflowCue.Show(overflow, OverflowCue.Hidden(row.GetChildren().OfType<Control>()
                .Select(slot => (slot.Position.X, slot.Position.X + slot.Size.X)), left, left + scroll.Size.X));
        }).CallDeferred();
        row.MinimumSizeChanged += MeasureOverflow;
        scroll.Resized += MeasureOverflow;
        scroll.GetHScrollBar().ValueChanged += _ => MeasureOverflow();
        MeasureOverflow();
        return controls;
    }
}



