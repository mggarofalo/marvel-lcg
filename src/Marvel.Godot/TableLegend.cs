using Godot;

namespace Marvel.Godot;

/// <summary>
/// The separated table's legend: each column icon beside its short label, explained on hover. The first time a
/// game's table separates it shows as a strip in the tracker line until the player closes it; the rules toggle
/// always holds it too.
/// </summary>
/// <remarks>
/// Which facts the legend lists and when it shows are presentation choices. Its strip lasts across re-renders of
/// one game, so a table update does not close it before the player does, and it stands in the tracker line so it
/// never covers a game area.
/// </remarks>
internal static class TableLegend
{
    private static bool seen;
    private static bool open;

    /// <summary>The facts the legend explains, in the order a player meets them on the table.</summary>
    internal static readonly TableFact[] Facts =
    [
        TableFact.YourArea, TableFact.OutOfReach, TableFact.Shared, TableFact.Acting,
        TableFact.Departing, TableFact.Destination, TableFact.Retained, TableFact.StoredDominions,
    ];

    /// <summary>Whether the strip shows on this render: on the game's first separated render, then until closed.</summary>
    internal static bool OpensNow()
    {
        if (!seen) open = true;
        seen = true;
        return open;
    }

    /// <summary>Records the player closing the strip.</summary>
    internal static void Closed() => open = false;

    /// <summary>Forgets the legend for a new game.</summary>
    internal static void Clear()
    {
        seen = false;
        open = false;
    }

    /// <summary>The legend: one icon and short label per fact, each explained on hover, in rows or along one line.</summary>
    internal static BoxContainer Create(InterfaceScale scale, bool strip = false)
    {
        BoxContainer rows = strip ? new HBoxContainer { Name = "TableLegendStrip" } : new VBoxContainer { Name = "TableLegend" };
        rows.MouseFilter = Control.MouseFilterEnum.Ignore;
        rows.AddThemeConstantOverride("separation", strip ? VisualSystem.Spacing(scale).Medium : VisualSystem.Spacing(scale).ExtraSmall);
        foreach (TableFact fact in Facts)
        {
            Wording wording = TableWording.For(fact);
            var row = new HBoxContainer { Name = "Legend" + fact, TooltipText = wording.Tooltip,
                AccessibilityName = wording.Label, AccessibilityDescription = wording.Tooltip,
                MouseFilter = Control.MouseFilterEnum.Pass };
            row.AddThemeConstantOverride("separation", VisualSystem.Spacing(scale).Small);
            row.AddChild(TableIcon.Create(wording.Icon, TableBlockStyle.Icon(scale), ClientTheme.ToGodot(VisualSystem.Palette.Text)));
            Label label = TableBlockStyle.Whole("Label", wording.Label, scale, VisualSystem.Palette.Text);
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(label);
            rows.AddChild(row);
        }
        return rows;
    }

    /// <summary>Shows only the strip's icons, each still named on hover, when its labels do not fit <paramref name="width"/>.</summary>
    internal static void FitStrip(BoxContainer strip, float width)
    {
        if (strip.GetCombinedMinimumSize().X <= width) return;
        foreach (Node row in strip.GetChildren())
            if (row.GetNodeOrNull<Label>("Label") is { } label) label.Visible = false;
    }
}
