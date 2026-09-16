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
            Text = $"{PileGlyph(area.Zone)}\n{pile.Count}\n{ShortName(area.Title)}",
            Disabled = pile.InspectionOrder.Count == 0,
            TooltipText = pile.InspectionOrder.Count == 0
                ? $"{pile.Count} concealed cards"
                : $"Inspect {area.Title.ToLowerInvariant()}, top card first.",
        };
        inspect.Pressed += () => TabletopPileInspector.Show(inspect, pile, result, scale, art);
        panel.AddChild(inspect);
        surface.AddChild(panel);
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
        int index = 0;
        foreach (BoardAreaPresentation area in areas)
        {
            TabletopAreaObject compact = TabletopAreaObject.From(area);
            var button = new Button
            {
                Name = $"InspectRegion{area.Id}",
                Text = $"{area.Title}  ·  {compact.Count}",
                TooltipText = $"Open {area.Title.ToLowerInvariant()} without leaving the table.",
                Position = slot.Position + new Vector2(0, index * 46),
                Size = new Vector2(slot.Size.X, 40),
                ClipText = true,
                ZIndex = 30,
            };
            button.Pressed += () => TabletopPileInspector.Show(button, compact, result, scale, art);
            surface.AddChild(button);
            index++;
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

    private static string ShortName(string title) => title
        .Replace("PLAYER ", string.Empty, StringComparison.Ordinal)
        .Replace("ENCOUNTER ", string.Empty, StringComparison.Ordinal);
}
