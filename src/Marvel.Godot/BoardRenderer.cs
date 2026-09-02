using Godot;

namespace Marvel.Godot;

/// <summary>Builds Godot controls from a visibility-safe board presentation.</summary>
public static class BoardRenderer
{
    /// <summary>Replaces the visible board with one authoritative snapshot.</summary>
    public static BoardRenderResult Render(GridContainer destination, BoardPresentation board)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(board);
        foreach (Node child in destination.GetChildren())
        {
            destination.RemoveChild(child);
            child.QueueFree();
        }

        var result = new BoardRenderResult();
        foreach (BoardAreaPresentation area in board.Areas)
        {
            PanelContainer control = Area(area, result);
            destination.AddChild(control);
        }

        return result;
    }

    private static PanelContainer Area(
        BoardAreaPresentation area,
        BoardRenderResult result)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(360, 120),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = $"{area.Title}. {area.Context}",
            ThemeTypeVariation = GodotThemeVariations.BoardArea,
        };

        var content = new VBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(content);
        content.AddChild(Label(area.Title, GodotThemeVariations.Heading));
        content.AddChild(Label(area.Context, GodotThemeVariations.Eyebrow));
        content.AddChild(new HSeparator());
        AddCards(content, area.Cards, "CARDS", result);
        if (area.Removed.Count > 0)
        {
            content.AddChild(new HSeparator());
            AddCards(content, area.Removed, "REMOVED", result);
        }

        return panel;
    }

    private static void AddCards(
        VBoxContainer destination,
        IReadOnlyList<BoardCardPresentation> cards,
        string section,
        BoardRenderResult result)
    {
        destination.AddChild(Label(
            $"{section}  ·  {cards.Sum(card => card.Count)}",
            GodotThemeVariations.Caption));
        if (cards.Count == 0)
        {
            destination.AddChild(Label("Empty", GodotThemeVariations.MutedText));
            return;
        }

        foreach (BoardCardPresentation card in cards)
        {
            PanelContainer control = Card(card);
            destination.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control, control.ThemeTypeVariation);
            }
        }
    }

    private static PanelContainer Card(BoardCardPresentation card)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = card.Title,
            ThemeTypeVariation = card.Concealed
                ? GodotThemeVariations.ConcealedCard
                : GodotThemeVariations.BoardCard,
        };

        var content = new VBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(content);
        content.AddChild(Label(
            card.Kind,
            card.Concealed
                ? GodotThemeVariations.Caption
                : GodotThemeVariations.Eyebrow));
        content.AddChild(Label(card.Title, GodotThemeVariations.Body, wrap: true));
        if (!string.IsNullOrWhiteSpace(card.Subtitle))
        {
            content.AddChild(Label(
                card.Subtitle,
                GodotThemeVariations.MutedText,
                wrap: true));
        }

        if (card.Fields.Count > 0)
        {
            var fields = new HFlowContainer();
            foreach (BoardFieldPresentation field in card.Fields)
            {
                fields.AddChild(Label(
                    $"{field.Name}  {field.Value}",
                    GodotThemeVariations.Caption));
            }

            content.AddChild(fields);
        }

        content.AddChild(Label(card.Status, GodotThemeVariations.Caption));
        return panel;
    }

    private static Label Label(string text, string variation, bool wrap = false)
    {
        return new Label
        {
            Text = text,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            ThemeTypeVariation = variation,
        };
    }
}

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<BoardControl>> controls = [];

    internal void Register(int id, Control control, string variation)
    {
        if (!controls.TryGetValue(id, out List<BoardControl>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(new BoardControl(control, variation));
    }

    /// <summary>Highlights every visible control matching one server-provided id.</summary>
    public void Highlight(int? id)
    {
        foreach ((int key, List<BoardControl> matches) in controls)
        {
            foreach (BoardControl match in matches)
            {
                match.Control.ThemeTypeVariation = id == key
                    ? GodotThemeVariations.FocusedCard
                    : match.Variation;
            }
        }
    }

    private sealed record BoardControl(Control Control, string Variation);
}
