using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Continuously scrollable physical cards with visible overflow and no object picker.</summary>
internal static class SpatialCardCollection
{
    internal static IReadOnlyList<CardControl> Render(Control surface, Rect2 bounds, string zone,
        BoardCardPresentation[] cards, BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art)
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
            CardLayoutMetrics layout = CardControl.LayoutFor(card, CardDisplaySize.Board, scale);
            float factor = Math.Min(1, (bounds.Size.Y - 40 - 48) / (layout.MinimumHeight + 32));
            CardControl control = CardControl.CreateFitted(card, scale, art, factor);
            control.Name = $"ProceduralCard{card.TargetId}";
            SpatialCardSidecar.State(control, card);
            controls.Add(control);
            bool exhausted = SpatialTableZones.IsExhausted(card);
            Vector2 face = control.CustomMinimumSize;
            Vector2 occupied = exhausted ? new Vector2(face.Y, face.X) : face;
            var slot = new Control { CustomMinimumSize = occupied + new Vector2(0, 32 * factor + 48),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            row.AddChild(slot);
            slot.AddChild(control);
            control.Position = new Vector2(0, 32 * factor);
            if (exhausted)
            {
                control.Rotation = Mathf.Pi / 2;
                control.Position = new Vector2(face.Y, 32 * factor);
                control.SetMeta("spatial_exhausted", true);
            }
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
            row.SortChildren += () => Callable.From(() =>
            { if (InteractionControl.IsUsable(control)) result.UpdateRestingPose(control); }).CallDeferred();
        }
        var overflow = new Label { Name = "ContinuousOverflow", Text = "Scroll to see all →",
            ThemeTypeVariation = GodotThemeVariations.Caption, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(overflow);
        row.MinimumSizeChanged += () => overflow.Visible = row.GetCombinedMinimumSize().X > bounds.Size.X;
        overflow.Visible = cards.Length > 1;
        return controls;
    }
}



