using Godot;

namespace Marvel.Godot;

/// <summary>The readable text and frames around separated game areas; wording beyond engine prose is ours.</summary>
internal static class GameAreaTableChrome
{
    /// <summary>Adds the progress header, with a toggle for the whole-table rules, and returns its measured height.</summary>
    internal static float AddHeader(Control surface, Vector2 position, float width, GameAreaTable table)
    {
        float rulesWidth = table.Rules.Count > 0 ? 200 : 0;
        float height = AddMeasured(surface, "KangProgress", position, width - rulesWidth - 8, table.Progress).Size.Y;
        if (table.Rules.Count == 0) return Math.Max(height, 0);
        var panel = new PanelContainer
        {
            Name = "SeparatedTableRulesPanel", Visible = false, ZIndex = 80,
            Position = new Vector2(position.X + width - 420, position.Y + 34), CustomMinimumSize = new Vector2(420, 0),
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
            Position = new Vector2(position.X + width - rulesWidth, position.Y), Size = new Vector2(rulesWidth, 30),
            TooltipText = "Show the rules that apply while the table is separated.",
        };
        toggle.Toggled += pressed => panel.Visible = pressed;
        surface.AddChild(toggle);
        surface.AddChild(panel);
        TableCompactButtonStyle.Apply(toggle);
        return Math.Max(height, 30);
    }

    /// <summary>Adds the progress line for a Kang table that is not separated.</summary>
    internal static void AddBanner(Control surface, AstraTableGeometry geometry, GameAreaTable table)
    {
        if (table.Progress.Length == 0) return;
        AddMeasured(surface, "KangProgress", new Vector2(geometry.MainScheme.Position.X, geometry.VillainMat.Position.Y + 4),
            geometry.VillainMat.End.X - geometry.MainScheme.Position.X - 16, table.Progress);
    }

    /// <summary>Marks the encounter piles as shared; the engine's full sentence is the tooltip.</summary>
    internal static void AddSharedCaption(Control surface, Vector2 position, float width, string note) =>
        AddMeasured(surface, "SharedEncounterNote", position, width, "SHARED BY ALL AREAS").TooltipText = note;

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

    /// <summary>Adds a column's explanation sentences, unplaced until the column is arranged.</summary>
    internal static GameAreaDetailsText? AddDetails(Control surface, string name, float width, IReadOnlyList<string> details)
    {
        if (details.Count == 0) return null;
        var viewport = new ScrollContainer
        {
            Name = name + "Viewport", Size = new Vector2(width, 0), ZIndex = 12,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        var text = new Label
        {
            Name = name, Text = string.Join("\n", details), AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(width - 12, 0), ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        viewport.AddChild(text);
        surface.AddChild(viewport);
        viewport.AddToGroup(ActivityConnectorOcclusion.TextGroup);
        return new GameAreaDetailsText(viewport, text);
    }

    /// <summary>Adds wrapped caption text sized to its measured height at the given width, so it is never clipped.</summary>
    internal static Label AddMeasured(Control surface, string name, Vector2 position, float width, string text)
    {
        var label = new Label
        {
            Name = name, Text = text, Position = position, CustomMinimumSize = new Vector2(width, 0),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = GodotThemeVariations.Caption,
            TooltipText = text, ZIndex = 12, MouseFilter = Control.MouseFilterEnum.Pass,
        };
        surface.AddChild(label);
        label.AddToGroup(ActivityConnectorOcclusion.TextGroup);
        label.Size = new Vector2(width, label.GetCombinedMinimumSize().Y);
        return label;
    }
}
