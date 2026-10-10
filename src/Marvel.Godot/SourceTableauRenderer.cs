using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Every installed controlled source has an independent, directly actionable tile.</summary>
internal static class SourceTableauRenderer
{
    internal static Control Create(BoardCardPresentation[] cards, Rect2 bounds,
        BoardRenderResult result, InterfaceScale scale)
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
        var grid = new Control { Name = "SourceTableauGrid",
            CustomMinimumSize = new Vector2(bounds.Size.X - 12,
                (int)Math.Ceiling((double)cards.Length / layout.Columns) * (SourceTableauLayout.TileHeight + SourceTableauLayout.Gap)),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        scroll.AddChild(grid);
        result.Inspector.Register(cards);
        for (int index = 0; index < cards.Length; index++)
        {
            BoardCardPresentation card = cards[index];
            CardControl control = SourceTableauTile.CreateCard(card, layout.TileWidth, scale);
            control.Name = $"ProceduralCard{card.TargetId}";
            control.Position = layout.Position(index);
            grid.AddChild(control);
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
        }
        return panel;
    }
}
