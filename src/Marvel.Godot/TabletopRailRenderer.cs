using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds the far-side and near-side physical rails from a tabletop rail plan.</summary>
internal static class TabletopRailRenderer
{
    internal static PanelContainer Rail(
        string name,
        string title,
        IReadOnlyList<BoardAreaPresentation> areas,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art,
        int? dropSeat = null)
    {
        var panel = new PanelContainer
        {
            Name = name,
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label(title, GodotThemeVariations.Eyebrow));
        var scroll = new ScrollContainer
        {
            Name = "RailScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FollowFocus = true,
        };
        var row = new HBoxContainer
        {
            Name = "Rail",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(row);
        stack.AddChild(scroll);
        panel.AddChild(stack);
        if (dropSeat is { } seat)
        {
            result.RegisterDropTarget(seat, panel);
        }

        foreach (BoardAreaPresentation area in areas)
        {
            row.AddChild(Area(area, result, scale, art, compact: false));
        }
        if (areas.Count == 0)
        {
            row.AddChild(Label("No live areas.", GodotThemeVariations.MutedText));
        }
        return panel;
    }

    internal static PanelContainer Shelf(
        string name,
        string title,
        IReadOnlyList<BoardAreaPresentation> areas,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        var panel = new PanelContainer
        {
            Name = name,
            ThemeTypeVariation = GodotThemeVariations.TabletopShelf,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label($"{title}  ·  {areas.Count}", GodotThemeVariations.Caption));
        var scroll = new ScrollContainer
        {
            Name = "ShelfScroll",
            CustomMinimumSize = new Vector2(0, VisualSystem.Card(CardDisplaySize.Hand, scale).MinimumHeight + 22),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FollowFocus = true,
        };
        var row = new HBoxContainer
        {
            Name = "Shelf",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(row);
        stack.AddChild(scroll);
        panel.AddChild(stack);
        foreach (BoardAreaPresentation area in areas)
        {
            row.AddChild(Area(area, result, scale, art, compact: true));
        }
        return panel;
    }

    private static PanelContainer Area(
        BoardAreaPresentation area,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art,
        bool compact)
    {
        CardDisplaySize size = compact ? CardDisplaySize.Hand : CardDisplaySize.Board;
        int count = area.Cards.Sum(card => card.Count) + area.Removed.Sum(card => card.Count);
        var panel = new PanelContainer
        {
            Name = $"Area{area.Id}",
            CustomMinimumSize = new Vector2(
                VisualSystem.Card(size, scale).Width + 28,
                VisualSystem.Card(size, scale).MinimumHeight + 42),
            SizeFlagsHorizontal = compact
                ? Control.SizeFlags.ShrinkBegin
                : Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
            TooltipText = $"{area.Title}. {area.Context}",
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label($"{area.Title}  ·  {count}", GodotThemeVariations.Caption, wrap: true));
        var cards = new HBoxContainer
        {
            Name = "Cards",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        AddCards(cards, area.Cards.Concat(area.Removed), result, size, scale, art);
        if (cards.GetChildCount() == 0)
        {
            cards.AddChild(Label("Empty", GodotThemeVariations.MutedText));
        }
        stack.AddChild(cards);
        panel.AddChild(stack);
        return panel;
    }

    private static void AddCards(
        Container destination,
        IEnumerable<BoardCardPresentation> cards,
        BoardRenderResult result,
        CardDisplaySize size,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        foreach (BoardCardPresentation card in cards)
        {
            CardControl control = CardControl.Create(card, size, scale, art);
            destination.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card);
        }
    }

    private static Label Label(string text, string variation, bool wrap = false) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
    };
}
