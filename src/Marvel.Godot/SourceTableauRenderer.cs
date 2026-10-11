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
        var panel = new VBoxContainer { Name = "SourceTableau", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeConstantOverride("separation", 4);
        panel.AddChild(new Label { Name = "SourceTableauTitle", Text = $"UPGRADES & SUPPORTS · {cards.Length}",
            ThemeTypeVariation = GodotThemeVariations.Caption, MouseFilter = Control.MouseFilterEnum.Ignore });
        var scroll = new ScrollContainer { Name = "SourceTableauViewport",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto, FollowFocus = true };
        panel.AddChild(scroll);
        var layout = new SourceTableauLayout(bounds.Size.X - 12, bounds.Size.Y - 28, cards.Length);
        var grid = new GridContainer { Name = "SourceTableauGrid", Columns = layout.Columns,
            CustomMinimumSize = new Vector2(bounds.Size.X - 12,
                (int)Math.Ceiling((double)cards.Length / layout.Columns) * (SourceTableauLayout.TileHeight + SourceTableauLayout.Gap)),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", (int)SourceTableauLayout.Gap);
        grid.AddThemeConstantOverride("v_separation", (int)SourceTableauLayout.Gap);
        scroll.AddChild(grid);
        var gestures = new SourceGridGesture { Name = "SourceGridGesture" };
        panel.AddChild(gestures);
        gestures.Configure(grid, scroll, seat, result);
        result.Inspector.Register(cards);
        for (int index = 0; index < cards.Length; index++)
        {
            BoardCardPresentation card = cards[index];
            CardControl control = SourceTableauTile.CreateCard(card, layout.TileWidth, scale);
            control.Name = $"ProceduralCard{card.TargetId}";
            control.Position = layout.Position(index);
            if (card.Persistent?.Relation.HostId is { } host && names?.TryGetValue(host, out string? hostName) == true)
            {
                var relation = CardSourceStrip.Label($"On {hostName}", "SourceHost", 9, CardTypography.Body);
                relation.ClipText = true;
                relation.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
                relation.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                relation.TooltipText = $"Attached to {hostName}";
                var row = control.FindChild("SourceType", true, false).GetParent();
                row.AddChild(relation);
                control.TooltipText += $" · Attached to {hostName}";
            }
            var slot = new Control { Name = $"SourceSlot{card.TargetId}", CustomMinimumSize = control.CustomMinimumSize,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            grid.AddChild(slot);
            control.Position = Vector2.Zero;
            slot.AddChild(control);
            BindSlot(control, slot);
            gestures.Track(control, slot, card.Title);
            grid.SortChildren += () => Callable.From(() =>
            { if (InteractionControl.IsUsable(control) && !control.TopLevel) result.UpdateRestingPose(control); }).CallDeferred();
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
        }
        return panel;
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
