using Godot;

namespace Marvel.Godot;

/// <summary>
/// The separated table's legend: each column icon beside its short label, explained on hover. It opens by itself
/// the first time a game's table separates, and afterwards only from the rules toggle that holds it.
/// </summary>
/// <remarks>
/// Which facts the legend lists and when it opens are presentation choices. Its open state lasts across
/// re-renders of one game, so a table update does not close it before the player does.
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

    /// <summary>Whether the legend shows on this render: on the game's first separated render, then while left open.</summary>
    internal static bool OpensNow()
    {
        if (!seen) open = true;
        seen = true;
        return open;
    }

    /// <summary>Records the player opening or closing the legend.</summary>
    internal static void Toggled(bool pressed) => open = pressed;

    /// <summary>Forgets the legend for a new game.</summary>
    internal static void Clear()
    {
        seen = false;
        open = false;
    }

    /// <summary>The legend's rows: one icon and short label per fact, each explained on hover.</summary>
    internal static VBoxContainer Create(InterfaceScale scale)
    {
        var rows = new VBoxContainer { Name = "TableLegend", MouseFilter = Control.MouseFilterEnum.Ignore };
        rows.AddThemeConstantOverride("separation", VisualSystem.Spacing(scale).ExtraSmall);
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
}
