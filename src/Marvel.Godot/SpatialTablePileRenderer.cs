using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders spatial deck piles and bounded drawers without interpreting their contents.</summary>
internal sealed class SpatialTablePileRenderer(
    Control surface,
    BoardRenderResult result,
    InterfaceScale scale,
    ICardArtProvider? art,
    bool openingMulligan)
{
    internal bool Render(IReadOnlyList<BoardAreaPresentation> areas, string zone, Rect2 rect)
    {
        BoardAreaPresentation? area = areas.FirstOrDefault(candidate => candidate.Zone == zone);
        if (area is null)
        {
            return false;
        }
        TabletopAreaObject pile = TabletopAreaObject.From(area);
        var panel = PilePanel($"Pile{area.Id}", rect, $"{area.Title}. {area.Context}");
        var inspect = new Button
        {
            Name = $"InspectPile{area.Id}",
            Text = $"{PileGlyph(area.Zone)}\n{pile.Count}\n{(area.Zone.Contains("Discard", StringComparison.Ordinal) ? "Discard" : "Deck")}",
            ClipText = true,
            AccessibilityName = TabletopAreaNames.Title(area.Title),
            Disabled = pile.InspectionOrder.Count == 0,
            TooltipText = pile.InspectionOrder.Count == 0
                ? $"{pile.Count} concealed cards"
                : $"Inspect {area.Title.ToLowerInvariant()}, top card first.",
        };
        inspect.Pressed += () => TabletopPileInspector.Show(inspect, pile, result, scale, art);
        panel.AddChild(inspect);
        surface.AddChild(panel);
        TableCompactButtonStyle.Apply(inspect);
        if (openingMulligan && area.Zone == "DiscardPile")
        {
            result.RegisterMulliganDiscard(panel);
        }
        return true;
    }

    internal void RenderEmpty(string name, string text, Rect2 rect, bool mulliganDiscard)
    {
        var panel = PilePanel(
            $"PileEmpty{name}",
            rect,
            mulliganDiscard
                ? "Drop an opening-hand card here to select it for replacement."
                : "Empty pile.");
        panel.AddChild(new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        surface.AddChild(panel);
        if (mulliganDiscard)
        {
            result.RegisterMulliganDiscard(panel);
        }
    }

    internal void RenderOverflow(IReadOnlyList<BoardAreaPresentation> areas, Rect2 slot)
    {
        if (areas.Count == 0) return;
        var drawer = new MenuButton
        {
            Name = "InspectOtherRegions", Text = "Other piles",
            TooltipText = "Inspect other areas and their visible contents.",
            Position = slot.Position, Size = slot.Size,
            ClipText = true, ZIndex = 30,
        };
        PopupMenu menu = drawer.GetPopup();
        for (int index = 0; index < areas.Count; index++)
        {
            BoardAreaPresentation area = areas[index];
            menu.AddItem($"{area.Title} · {TabletopAreaObject.From(area).Count}", index);
        }
        menu.IdPressed += id => TabletopPileInspector.Show(drawer,
            TabletopAreaObject.From(areas[(int)id]), result, scale, art);
        surface.AddChild(drawer);
    }

    internal void RenderRegion(IReadOnlyList<BoardAreaPresentation> areas, string zone, Rect2 slot,
        CardControl? host = null)
    {
        BoardCardPresentation[] cards = [.. areas.SelectMany(SpatialTableZones.Current)];
        var drawer = new MenuButton
        {
            Name = $"InspectRegion{zone}",
            Text = $"{TabletopAreaNames.Region(zone)}\n{cards.Length}",
            TooltipText = "Inspect a card and use its offered actions through the current draft.",
            Position = slot.Position, Size = slot.Size,
            ClipText = false, ZIndex = 30, Flat = false,
            AccessibilityName = $"Inspect {TabletopAreaNames.Region(zone)} · {cards.Length} cards",
            CustomMinimumSize = new Vector2(0, 44),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.ChoiceButton,
        };
        drawer.AddThemeFontSizeOverride("font_size", 14);
        var choices = new List<(TabletopAreaObject Pile, int Index)>();
        PopupMenu menu = drawer.GetPopup();
        foreach (BoardAreaPresentation area in areas)
        {
            TabletopAreaObject pile = TabletopAreaObject.From(area with { Removed = [] });
            for (int index = 0; index < pile.InspectionOrder.Count; index++)
            {
                menu.AddItem($"{index + 1} · {CardStatePresentation.Summary(pile.InspectionOrder[index])}", choices.Count);
                choices.Add((pile, index));
            }
        }
        menu.IdPressed += id =>
        {
            if (result.IsCurrent?.Invoke() != true) return;
            var choice = choices[(int)id];
            TabletopPileInspector.Show(drawer, choice.Pile, result, scale, art, choice.Index);
        };
        if (host is null) surface.AddChild(drawer);
        else
        {
            drawer.Position = Vector2.Zero;
            drawer.ZAsRelative = true;
            drawer.ZIndex = 0;
            SpatialCardSidecar.For(host).AddChild(drawer);
            Callable.From(() => SpatialCardSidecar.Place(host)).CallDeferred();
        }
    }

    private static PanelContainer PilePanel(string name, Rect2 rect, string tooltip) => new()
    {
        Name = name,
        Position = rect.Position,
        Size = rect.Size,
        CustomMinimumSize = rect.Size,
        ThemeTypeVariation = GodotThemeVariations.SpatialPile,
        TooltipText = tooltip,
        ZIndex = 6,
    };

    private static string PileGlyph(string zone) =>
        zone.Contains("Discard", StringComparison.Ordinal) ? "▱" : "▰";

}
