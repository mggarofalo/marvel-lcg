using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Every installed controlled source has an independent, directly actionable tile.</summary>
internal static class SourceTableauRenderer
{
    internal static Control Create(BoardCardPresentation[] cards, Rect2 bounds,
        BoardRenderResult result, InterfaceScale scale, IReadOnlyDictionary<int, string>? names = null)
    {
        var panel = new VBoxContainer { Name = "SourceTableau", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeConstantOverride("separation", 4);
        panel.AddChild(new Label { Name = "SourceTableauTitle", Text = $"UPGRADES & SUPPORTS · {cards.Length}",
            ThemeTypeVariation = GodotThemeVariations.Caption, MouseFilter = Control.MouseFilterEnum.Ignore });
        var scroll = new ScrollContainer { Name = "SourceTableauViewport",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto, FollowFocus = true };
        panel.AddChild(scroll);
        var layout = new SourceTableauLayout(bounds.Size.X - 12);
        var grid = new GridContainer { Name = "SourceTableauGrid", Columns = layout.Columns,
            CustomMinimumSize = new Vector2(bounds.Size.X - 12,
                (int)Math.Ceiling((double)cards.Length / layout.Columns) * (SourceTableauLayout.TileHeight + SourceTableauLayout.Gap)),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", (int)SourceTableauLayout.Gap);
        grid.AddThemeConstantOverride("v_separation", (int)SourceTableauLayout.Gap);
        scroll.AddChild(grid);
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
                relation.AutowrapMode = TextServer.AutowrapMode.Word;
                control.GetNode<VBoxContainer>("SourceBody").AddChild(relation);
            }
            grid.AddChild(control);
            grid.SortChildren += () => Callable.From(() =>
            { if (InteractionControl.IsUsable(control)) result.UpdateRestingPose(control); }).CallDeferred();
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
        }
        return panel;
    }
}
