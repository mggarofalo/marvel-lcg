using Godot;

namespace Marvel.Godot;

/// <summary>The readable text and frames around separated game areas; wording beyond engine prose is ours.</summary>
internal static class GameAreaTableChrome
{
    /// <summary>Adds the progress and acting header, with a toggle for the whole-table rules.</summary>
    internal static void AddHeader(Control surface, Rect2 bounds, GameAreaTable table)
    {
        float rulesWidth = table.Rules.Count > 0 ? 200 : 0;
        string text = table.Acting is { } acting ? $"{table.Progress}\n{acting}" : table.Progress;
        AddText(surface, "KangProgress", new Rect2(bounds.Position, bounds.Size - new Vector2(rulesWidth + 8, 0)), text);
        if (table.Rules.Count == 0) return;
        var panel = new PanelContainer
        {
            Name = "SeparatedTableRulesPanel", Visible = false, ZIndex = 80,
            Position = new Vector2(bounds.End.X - 420, bounds.End.Y + 2), CustomMinimumSize = new Vector2(420, 0),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        using var style = new StyleBoxFlat { BgColor = ClientTheme.ToGodot(VisualSystem.Palette.RaisedSurface),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(new Label
        {
            Name = "SeparatedTableRulesText", Text = string.Join("\n\n", table.Rules),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(400, 0),
            ThemeTypeVariation = GodotThemeVariations.Caption, MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        var toggle = new Button
        {
            Name = "SeparatedTableRules", Text = "Separated-table rules", ToggleMode = true, ZIndex = 20,
            Position = new Vector2(bounds.End.X - rulesWidth, bounds.Position.Y), Size = new Vector2(rulesWidth, 30),
            TooltipText = "Show the rules that apply while the table is separated.",
        };
        toggle.Toggled += pressed => panel.Visible = pressed;
        surface.AddChild(toggle);
        surface.AddChild(panel);
        TableCompactButtonStyle.Apply(toggle);
    }

    /// <summary>Adds the progress line for a Kang table that is not separated.</summary>
    internal static void AddBanner(Control surface, AstraTableGeometry geometry, GameAreaTable table)
    {
        if (table.Progress.Length == 0) return;
        AddText(surface, "KangProgress", new Rect2(geometry.MainScheme.Position.X, geometry.VillainMat.Position.Y + 4,
            geometry.VillainMat.End.X - geometry.MainScheme.Position.X - 16, 24), table.Progress);
    }

    /// <summary>Marks the encounter piles as shared; the engine's full sentence is the tooltip.</summary>
    internal static void AddSharedCaption(Control surface, Rect2 bounds, string note)
    {
        AddText(surface, "SharedEncounterNote", bounds, "SHARED BY EVERY GAME AREA")!.TooltipText = note;
    }

    /// <summary>Adds a bordered column; the acting and inspected-seat areas carry a heavier border.</summary>
    internal static void AddFrame(Control surface, string name, Rect2 bounds, GameAreaColumn column)
    {
        bool acting = column.Markers.Contains(GameAreaMarker.Acting);
        bool yours = column.Markers.Contains(GameAreaMarker.Yours);
        VisualColor edge = acting ? VisualSystem.Palette.Danger : yours ? VisualSystem.Palette.Legal : VisualSystem.Palette.Outline;
        int width = acting ? 4 : yours ? 2 : 1;
        var frame = new Panel
        {
            Name = name, Position = bounds.Position, Size = bounds.Size, ZIndex = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        using var style = new StyleBoxFlat
        {
            BgColor = new Color(ClientTheme.ToGodot(VisualSystem.Palette.Surface), 0.35f),
            BorderColor = ClientTheme.ToGodot(edge),
            BorderWidthLeft = width, BorderWidthRight = width, BorderWidthTop = width, BorderWidthBottom = width,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        };
        frame.AddThemeStyleboxOverride("panel", style);
        frame.SetMeta("game_area_markers", column.MarkerText);
        frame.AccessibilityDescription = $"{column.Heading}. {column.MarkerText}. {string.Join(" ", column.Details)}";
        surface.AddChild(frame);
    }

    /// <summary>Adds the explanation sentences below a column's cards, scrolling when they overflow.</summary>
    internal static void AddDetails(Control surface, string name, Rect2 bounds, IReadOnlyList<string> details)
    {
        if (details.Count == 0) return;
        var scroll = new ScrollContainer
        {
            Name = name + "Viewport", Position = bounds.Position, Size = bounds.Size, ZIndex = 12,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        scroll.AddChild(new Label
        {
            Name = name, Text = string.Join("\n", details), AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(bounds.Size.X - 12, 0), ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        surface.AddChild(scroll);
    }

    /// <summary>Adds wrapped caption text clipped to its bounds; the full text is also its tooltip.</summary>
    internal static Label? AddText(Control surface, string name, Rect2 bounds, string text)
    {
        if (text.Length == 0) return null;
        var clip = new Control
        {
            Name = name + "Clip", Position = bounds.Position, Size = bounds.Size, ClipContents = true, ZIndex = 12,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var label = new Label
        {
            Name = name, Text = text, CustomMinimumSize = new Vector2(bounds.Size.X, 0),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = GodotThemeVariations.Caption,
            TooltipText = text, MouseFilter = Control.MouseFilterEnum.Pass,
        };
        clip.AddChild(label);
        surface.AddChild(clip);
        return label;
    }
}
