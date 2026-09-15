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
        TabletopAreaObject presentation = TabletopAreaObject.From(area);
        if (presentation.IsPile)
        {
            return Pile(presentation, result, scale, art);
        }

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

    private static PanelContainer Pile(
        TabletopAreaObject pile,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        CardLayoutMetrics card = VisualSystem.Card(CardDisplaySize.Hand, scale);
        var panel = new PanelContainer
        {
            Name = $"Pile{pile.Area.Id}",
            CustomMinimumSize = new Vector2(card.Width, card.MinimumHeight + 46),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            ThemeTypeVariation = GodotThemeVariations.TabletopShelf,
            TooltipText = $"{pile.Area.Title}. {pile.Area.Context}",
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label(pile.Area.Title, GodotThemeVariations.Caption, wrap: true));
        var inspect = new Button
        {
            Name = $"InspectPile{pile.Area.Id}",
            Text = $"▰  {pile.Count} CARDS\n{pile.Detail}",
            Alignment = HorizontalAlignment.Left,
            Disabled = pile.InspectionOrder.Count == 0,
            TooltipText = pile.InspectionOrder.Count == 0
                ? $"{pile.Count} concealed cards"
                : $"Inspect {pile.Area.Title.ToLowerInvariant()}, top card first.",
        };
        inspect.Pressed += () => TabletopPileInspector.Show(inspect, pile, result, scale, art);
        stack.AddChild(inspect);
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
