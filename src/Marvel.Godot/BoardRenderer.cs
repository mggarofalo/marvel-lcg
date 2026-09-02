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
            CardControl control = CardControl.Create(
                card, CardDisplaySize.Board, ClientTheme.ConfiguredScale());
            destination.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
        }
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
    private readonly Dictionary<int, List<CardControl>> controls = [];

    internal void Register(int id, CardControl control)
    {
        if (!controls.TryGetValue(id, out List<CardControl>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(control);
    }

    /// <summary>Highlights every visible control matching one server-provided id.</summary>
    public void Highlight(int? id)
    {
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches)
            {
                match.SetHighlighted(id == key);
            }
        }
    }
}
