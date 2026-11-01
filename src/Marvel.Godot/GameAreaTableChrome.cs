using Godot;

namespace Marvel.Godot;

/// <summary>The separated table's tracker line, rules toggle and column frames, built from the building blocks.</summary>
internal static class GameAreaTableChrome
{
    private const float OutOfReachAlpha = 0.55f;

    /// <summary>Adds the tracker line across a band, with the rules toggle at its end; returns the line's height.</summary>
    internal static float AddTrackerLine(Control surface, Rect2 band, GameAreaTable table, InterfaceScale scale)
    {
        float height = ColumnHeader.Height(scale);
        HBoxContainer tracker = Tracker.Create("KangTracker", table.Tracker, scale);
        tracker.Position = band.Position;
        tracker.Size = new Vector2(band.Size.X / 2, height);
        tracker.ZIndex = 12;
        surface.AddChild(tracker);
        if (table.Rules.Count > 0) AddRules(surface, band, table, scale, height);
        return height;
    }

    /// <summary>Adds the tracker for a Kang table that is not separated, at the villain mat's corner.</summary>
    internal static void AddBanner(Control surface, AstraTableGeometry geometry, GameAreaTable table)
    {
        if (table.Tracker.Label.Length == 0) return;
        HBoxContainer tracker = Tracker.Create("KangTracker", table.Tracker, geometry.ReadingScale);
        // A caption line in the margin above the main scheme, so it never covers the card.
        int caption = VisualSystem.Type(geometry.ReadingScale).Caption;
        tracker.GetNode<Label>("Label").AddThemeFontSizeOverride("font_size", caption);
        tracker.Position = new Vector2(geometry.MainScheme.Position.X,
            Math.Max(geometry.VillainMat.Position.Y + 2, geometry.MainScheme.Position.Y - caption * 1.4f - 2));
        tracker.ZIndex = 12;
        surface.AddChild(tracker);
    }

    // The rules live behind one toggle: an icon and a short label, never ambient text on the table.
    private static void AddRules(Control surface, Rect2 band, GameAreaTable table, InterfaceScale scale, float height)
    {
        Wording rules = TableWording.For(TableFact.SeparatedRules);
        float width = Math.Min(band.Size.X * 0.45f, 460 * (int)scale / 100f);
        var panel = new PanelContainer
        {
            Name = "SeparatedTableRulesPanel", Visible = false, ZIndex = 80, MouseFilter = Control.MouseFilterEnum.Stop,
            Position = new Vector2(band.End.X - width, band.Position.Y + height + 4), CustomMinimumSize = new Vector2(width, 0),
        };
        using StyleBoxFlat style = TableBlockStyle.Surface(scale, VisualSystem.Palette.Outline);
        style.ContentMarginTop = style.ContentMarginBottom = style.ContentMarginLeft;
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(new Label
        {
            Name = "SeparatedTableRulesText", Text = string.Join("\n\n", table.Rules),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(width - 24, 0),
            ThemeTypeVariation = GodotThemeVariations.Caption, MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        var toggle = new Button
        {
            Name = "SeparatedTableRules", Text = rules.Label, ToggleMode = true, ZIndex = 20, TooltipText = rules.Tooltip,
            Icon = CardGlyphRendering.Rasterize($"Marvel.Godot.Assets.TableIcons.{rules.Icon}.svg", TableBlockStyle.Icon(scale)),
        };
        toggle.Toggled += pressed => panel.Visible = pressed;
        surface.AddChild(toggle);
        surface.AddChild(panel);
        TableCompactButtonStyle.Apply(toggle);
        toggle.Size = new Vector2(toggle.GetCombinedMinimumSize().X, height);
        toggle.Position = new Vector2(band.End.X - toggle.Size.X, band.Position.Y);
    }

    /// <summary>
    /// Adds a column's frame: the acting area takes a heavy danger edge, a lit destination the accent edge,
    /// and the inspected seat's own area the legal edge. Each edge repeats a header icon; none stands alone.
    /// </summary>
    internal static void AddFrame(Control surface, string name, Rect2 bounds, GameAreaColumn column)
    {
        (VisualColor edge, int width) = column.Facts.Contains(TableFact.Acting) ? (VisualSystem.Palette.Danger, 4)
            : column.Facts.Contains(TableFact.Destination) ? (VisualSystem.Palette.Accent, 3)
            : column.Facts.Contains(TableFact.YourArea) ? (VisualSystem.Palette.Legal, 2)
            : (VisualSystem.Palette.Outline, 1);
        var frame = new Panel
        {
            Name = name, Position = bounds.Position, Size = bounds.Size, ZIndex = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore, AccessibilityDescription = column.Tooltip,
        };
        using var style = new StyleBoxFlat
        {
            BgColor = new Color(ClientTheme.ToGodot(VisualSystem.Palette.Surface), column.Facts.Contains(TableFact.OutOfReach) ? 0.15f : 0.35f),
            BorderColor = ClientTheme.ToGodot(edge),
            BorderWidthLeft = width, BorderWidthRight = width, BorderWidthTop = width, BorderWidthBottom = width,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        };
        frame.AddThemeStyleboxOverride("panel", style);
        frame.SetMeta("table_facts", ColumnHeader.Keys(column.Facts));
        surface.AddChild(frame);
    }

    /// <summary>Adds a column's header; an acting area adds its step as a tracker of the phase's three steps.</summary>
    internal static HBoxContainer AddHeader(Control surface, string name, Rect2 bounds, GameAreaColumn column,
        InterfaceScale scale)
    {
        Control? step = column.ActingStep is { } acting
            ? Tracker.Create("ActingStep", new TrackerState("", StepIndex(acting), 3, null, 0,
                TableWording.For(TableWording.Step(acting)).Tooltip), scale)
            : null;
        // The step pips and the heavy border state that the area is acting, so its icon is not repeated.
        TableFact[] icons = [.. column.Facts.Where(fact => step is null || fact != TableFact.Acting)];
        HBoxContainer header = ColumnHeader.Create(name + "Heading", column.Title, icons, column.Tooltip, scale, step);
        header.Position = bounds.Position + new Vector2(GameAreaColumnLayout.Padding + 2, GameAreaColumnLayout.Padding);
        header.Size = new Vector2(bounds.Size.X - 2 * (GameAreaColumnLayout.Padding + 2), ColumnHeader.Height(scale));
        header.ZIndex = 12;
        if (column.Facts.Contains(TableFact.OutOfReach)) header.GetNode<Label>("Title").Modulate = new Color(1, 1, 1, OutOfReachAlpha + 0.2f);
        surface.AddChild(header);
        header.AddToGroup(ActivityConnectorOcclusion.TextGroup);
        return header;
    }

    /// <summary>How many of the villain phase's three focus steps (1, 2 and 4) have begun when <paramref name="step"/> is resolving.</summary>
    internal static int StepIndex(int step) => step switch
    {
        1 => 1,
        2 => 2,
        4 => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, "no villain-phase focus has this step"),
    };

    /// <summary>The alpha an out-of-reach card is drawn with.</summary>
    internal static float DimmedAlpha => OutOfReachAlpha;
}
