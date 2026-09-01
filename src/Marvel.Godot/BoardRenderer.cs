using Godot;

namespace Marvel.Godot;

/// <summary>Builds Godot controls from a visibility-safe board presentation.</summary>
public static class BoardRenderer
{
    private static readonly Color Ink = new("e8e4d8");
    private static readonly Color Muted = new("91a4a8");
    private static readonly Color Amber = new("e6a646");

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
        };
        panel.AddThemeStyleboxOverride("panel", PanelStyle());

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 9);
        panel.AddChild(content);
        content.AddChild(Label(area.Title, 18, Ink));
        content.AddChild(Label(area.Context, 10, Amber));
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
        destination.AddChild(Label($"{section}  ·  {cards.Sum(card => card.Count)}", 10, Muted));
        if (cards.Count == 0)
        {
            destination.AddChild(Label("Empty", 13, Muted));
            return;
        }

        foreach (BoardCardPresentation card in cards)
        {
            PanelContainer control = Card(card);
            destination.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
        }
    }

    private static PanelContainer Card(BoardCardPresentation card)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = card.Title,
        };
        panel.AddThemeStyleboxOverride("panel", CardStyle(card.Concealed));

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 3);
        panel.AddChild(content);
        content.AddChild(Label(card.Kind, 9, card.Concealed ? Muted : Amber));
        content.AddChild(Label(card.Title, 15, Ink, wrap: true));
        if (!string.IsNullOrWhiteSpace(card.Subtitle))
        {
            content.AddChild(Label(card.Subtitle, 11, Muted, wrap: true));
        }

        if (card.Fields.Count > 0)
        {
            var fields = new HFlowContainer();
            fields.AddThemeConstantOverride("h_separation", 12);
            fields.AddThemeConstantOverride("v_separation", 3);
            foreach (BoardFieldPresentation field in card.Fields)
            {
                fields.AddChild(Label($"{field.Name}  {field.Value}", 10, Ink));
            }

            content.AddChild(fields);
        }

        content.AddChild(Label(card.Status, 9, Muted));
        return panel;
    }

    private static Label Label(string text, int size, Color color, bool wrap = false)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
        };
        return label.WithTheme(size, color);
    }

    private static StyleBoxFlat PanelStyle() => new()
    {
        BgColor = new Color("13222b"),
        BorderColor = new Color("526e76", 0.48f),
        BorderWidthLeft = 1,
        BorderWidthTop = 1,
        BorderWidthRight = 1,
        BorderWidthBottom = 1,
        CornerRadiusTopLeft = 10,
        CornerRadiusTopRight = 10,
        CornerRadiusBottomLeft = 10,
        CornerRadiusBottomRight = 10,
        ContentMarginLeft = 16,
        ContentMarginTop = 14,
        ContentMarginRight = 16,
        ContentMarginBottom = 14,
    };

    private static StyleBoxFlat CardStyle(bool concealed) => new()
    {
        BgColor = concealed ? new Color("0b151c") : new Color("182c34"),
        BorderColor = concealed
            ? new Color("526e76", 0.32f)
            : new Color("d39c49", 0.34f),
        BorderWidthLeft = concealed ? 1 : 3,
        BorderWidthTop = 1,
        BorderWidthRight = 1,
        BorderWidthBottom = 1,
        CornerRadiusTopLeft = 6,
        CornerRadiusTopRight = 6,
        CornerRadiusBottomLeft = 6,
        CornerRadiusBottomRight = 6,
        ContentMarginLeft = 11,
        ContentMarginTop = 9,
        ContentMarginRight = 11,
        ContentMarginBottom = 9,
    };

    private static Label WithTheme(this Label label, int size, Color color)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<Control>> controls = [];

    internal void Register(int id, Control control)
    {
        if (!controls.TryGetValue(id, out List<Control>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(control);
    }

    /// <summary>Highlights every visible control matching one server-provided id.</summary>
    public void Highlight(int? id)
    {
        foreach ((int key, List<Control> matches) in controls)
        {
            Color tint = id == key ? new Color("ffe09a") : Colors.White;
            foreach (Control control in matches)
            {
                control.Modulate = tint;
            }
        }
    }
}
