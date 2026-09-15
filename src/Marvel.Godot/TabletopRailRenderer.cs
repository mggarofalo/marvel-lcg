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
        var row = new HFlowContainer
        {
            Name = "Rail",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stack.AddChild(row);
        panel.AddChild(stack);
        if (dropSeat is { } seat)
        {
            result.RegisterDropTarget(seat, panel);
        }

        // The set-aside/nemesis collection is setup context, not a live play
        // area. Its cards enter the table through engine events when relevant.
        BoardAreaPresentation[] visibleAreas = [.. areas.Where(area => area.Zone != "AsideDeck")];
        foreach (BoardAreaPresentation area in visibleAreas)
        {
            row.AddChild(Area(area, result, scale, art, compact: false));
        }
        if (visibleAreas.Length == 0)
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
        var row = new HFlowContainer
        {
            Name = "Shelf",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stack.AddChild(row);
        panel.AddChild(stack);
        foreach (BoardAreaPresentation area in areas)
        {
            row.AddChild(Area(area, result, scale, art, compact: true));
        }
        return panel;
    }

    internal static void ReplaceHeading(PanelContainer rail, Control heading)
    {
        ArgumentNullException.ThrowIfNull(rail);
        ArgumentNullException.ThrowIfNull(heading);
        var stack = (VBoxContainer)rail.GetChild(0);
        Node prior = stack.GetChild(0);
        stack.RemoveChild(prior);
        prior.QueueFree();
        stack.AddChild(heading);
        stack.MoveChild(heading, 0);
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
        int count = area.Cards
            .Where(card => card.StageRole != BoardStageRole.Upcoming)
            .Sum(card => card.Count)
            + area.Removed.Sum(card => card.Count);
        var panel = new PanelContainer
        {
            Name = $"Area{area.Id}",
            CustomMinimumSize = new Vector2(
                VisualSystem.Card(size, scale).Width + 8,
                VisualSystem.Card(size, scale).MinimumHeight + 20),
            SizeFlagsHorizontal = compact
                ? Control.SizeFlags.ShrinkBegin
                : Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
            TooltipText = $"{area.Title}. {area.Context}",
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label($"{area.Title}  ·  {count}", GodotThemeVariations.Caption, wrap: true));
        var cards = new HFlowContainer
        {
            Name = "Cards",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        BoardCardPresentation[] current = [.. area.Cards
            .Where(card => card.StageRole != BoardStageRole.Upcoming)];
        BoardCardPresentation[] upcoming = [.. area.Cards
            .Where(card => card.StageRole == BoardStageRole.Upcoming)];
        int renderedCards = current.Length + area.Removed.Count;
        int columns = Math.Clamp(renderedCards, 1, 3);
        float cardWidth = VisualSystem.Card(size, scale).Width;
        panel.CustomMinimumSize = new Vector2(
            cardWidth * columns + Math.Max(0, columns - 1) * 8 + 32,
            panel.CustomMinimumSize.Y);
        panel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        result.Inspector.Register([.. current.Concat(upcoming)]);
        AddCards(cards, current.Concat(area.Removed), result, size, scale, art);
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
        string title = pile.ContainsOnlyRemovedCards
            ? "REMOVED"
            : pile.Area.Zone == "AsideDeck" ? "SET-ASIDE" : pile.Area.Title;
        var panel = new PanelContainer
        {
            Name = $"Pile{pile.Area.Id}",
            CustomMinimumSize = new Vector2(Math.Min(120, card.Width), 72),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            ThemeTypeVariation = GodotThemeVariations.TabletopShelf,
            TooltipText = $"{pile.Area.Title}. {pile.Area.Context}",
        };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        stack.AddChild(Label(title, GodotThemeVariations.Caption, wrap: true));
        var inspect = new Button
        {
            Name = $"InspectPile{pile.Area.Id}",
            Text = $"▰  {pile.Count}",
            Alignment = HorizontalAlignment.Left,
            Disabled = pile.InspectionOrder.Count == 0,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
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
