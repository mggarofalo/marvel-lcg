using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>One seat on the tabletop rail: a portrait, an HP bar and badges, with the full account on hover.</summary>
/// <remarks>
/// The seat's roles, form, statuses and engaged enemies are facts from the published summary; the
/// tile states them as badges and icons rather than words. The portrait is the hero's initials, our
/// choice while no art pack is required.
/// </remarks>
internal static class SeatTile
{
    /// <summary>A portrait's initials: the first letters of the first two words, or a single word's first two letters.</summary>
    internal static string Initials(string hero)
    {
        string[] words = hero.Split([' ', '-', '.'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
            _ => $"{words[0][0]}{words[1][0]}".ToUpperInvariant(),
        };
    }

    /// <summary>The seat's roles in display order; an observed seat has none.</summary>
    internal static TableFact[] Roles(int seat, DisplayedSeatSelection selection) =>
    [
        .. selection.ActivePlayer == seat ? [TableFact.ActiveSeat] : Array.Empty<TableFact>(),
        .. selection.PromptOwner == seat ? [TableFact.AnsweringSeat] : Array.Empty<TableFact>(),
        .. selection.ViewedPrivateSeat == seat ? [TableFact.YourSeat] : Array.Empty<TableFact>(),
        .. selection.PublicFocusSeat == seat ? [TableFact.PublicFocus] : Array.Empty<TableFact>(),
    ];

    /// <summary>The fact for a published identity kind, or null for a kind that is neither form.</summary>
    internal static TableFact? Form(string? kind) => kind switch
    {
        "Hero" => TableFact.HeroForm,
        "AlterEgo" => TableFact.AlterEgoForm,
        _ => null,
    };

    /// <summary>The seat's hero name from its lane title, whose last part names the hero.</summary>
    internal static string Hero(string? laneTitle, int seat) =>
        laneTitle?.Split(" · ", StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } parts ? parts[^1] : $"Player {seat + 1}";

    /// <summary>The tile, as the seat's workspace switch.</summary>
    internal static Button Create(BoardPresentation board, PlayerSummaryDescriptor summary, DisplayedSeatSelection selection,
        InterfaceScale scale, string tooltip)
    {
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        string hero = Hero(board.Lanes.FirstOrDefault(item => item.Seat == summary.Seat)?.Title, summary.Seat);
        TableFact[] roles = Roles(summary.Seat, selection);
        var tile = new Button
        {
            Name = $"SeatSwitch{summary.Seat}", Disabled = summary.Seat == selection.ExpandedSeat,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = tooltip, AccessibilityName = hero,
            CustomMinimumSize = new Vector2(0, Height(scale)),
        };
        var body = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddThemeConstantOverride("separation", 0);
        body.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = body.OffsetTop = spacing.ExtraSmall;
        body.OffsetRight = body.OffsetBottom = -spacing.ExtraSmall;
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", spacing.Small);
        row.AddChild(Portrait(hero, roles.Contains(TableFact.AnsweringSeat), summary.Seat == selection.ExpandedSeat, scale));
        var facts = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        facts.AddThemeConstantOverride("separation", 0);
        facts.AddChild(NameLine(hero, roles, scale));
        facts.AddChild(SeatHealth.Create(summary.Health, SeatHealth.Maximum(board, summary.Identity), scale));
        row.AddChild(facts);
        body.AddChild(row);
        body.AddChild(Badges(board, summary, scale));
        tile.AddChild(body);
        return tile;
    }

    /// <summary>A tile's height: the portrait beside a name line and the HP bar, then room for two rows of badges.</summary>
    internal static float Height(InterfaceScale scale) =>
        VisualSystem.Type(scale).Caption * 1.4f + SeatHealth.Height(scale) + 2 * BadgeRow(scale) + 2 * VisualSystem.Spacing(scale).ExtraSmall;

    // A chip is one caption line or icon, whichever is taller, inside its surface's margins.
    private static float BadgeRow(InterfaceScale scale) =>
        Math.Max(VisualSystem.Type(scale).Caption * 1.4f, TableBlockStyle.Icon(scale)) + VisualSystem.Spacing(scale).ExtraSmall;

    private static PanelContainer Portrait(string hero, bool answering, bool inspected, InterfaceScale scale)
    {
        float side = VisualSystem.Type(scale).Caption * 1.4f + SeatHealth.Height(scale);
        var portrait = new PanelContainer
        {
            Name = "Portrait", CustomMinimumSize = new Vector2(side, side), MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        using StyleBoxFlat style = TableBlockStyle.Surface(scale,
            answering ? VisualSystem.Palette.Accent : inspected ? VisualSystem.Palette.Legal : VisualSystem.Palette.Outline,
            answering || inspected ? 3 : 1);
        portrait.AddThemeStyleboxOverride("panel", style);
        var initials = new Label
        {
            Text = Initials(hero), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        initials.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Heading);
        portrait.AddChild(initials);
        return portrait;
    }

    // The name, then the seat's roles as icons at the line's end.
    private static HBoxContainer NameLine(string hero, TableFact[] roles, InterfaceScale scale)
    {
        var line = new HBoxContainer { Name = "NameLine", MouseFilter = Control.MouseFilterEnum.Ignore };
        line.AddThemeConstantOverride("separation", VisualSystem.Spacing(scale).ExtraSmall);
        line.AddChild(TableBlockStyle.Line("Hero", hero.ToUpperInvariant(), scale, VisualSystem.Palette.Text));
        int icon = TableBlockStyle.Icon(scale);
        foreach (TableFact role in roles)
        {
            Wording wording = TableWording.For(role);
            TextureRect mark = TableIcon.Create(wording.Icon, icon, ClientTheme.ToGodot(VisualSystem.Palette.Accent));
            mark.Name = "Role" + role;
            mark.AccessibilityName = wording.Label;
            mark.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            line.AddChild(mark);
        }
        return line;
    }

    private static HFlowContainer Badges(BoardPresentation board, PlayerSummaryDescriptor summary, InterfaceScale scale)
    {
        var badges = new HFlowContainer { Name = "Badges", MouseFilter = Control.MouseFilterEnum.Ignore };
        badges.AddThemeConstantOverride("h_separation", VisualSystem.Spacing(scale).ExtraSmall);
        if (Form(summary.Form) is { } form) badges.AddChild(Chip.Create("Form", TableWording.For(form), null, scale));
        foreach (string status in summary.Statuses)
            badges.AddChild(StatusBadge.Supports(status) ? StatusBadge.Create(status, status, compact: true)
                : Chip.Create("Status" + status, new Wording("", status, status), null, scale));
        if (summary.EngagedEnemies.Count > 0)
            badges.AddChild(Chip.Create("Engaged", TableWording.For(TableFact.Engaged), summary.EngagedEnemies.Count, scale));
        int pending = board.PendingEncounterCount(summary.Seat);
        if (pending > 0) badges.AddChild(TableBlockStyle.Whole("PendingEncounters", PendingEncounterIndicator.Compact(pending).TrimStart(' ', '·'),
            scale, VisualSystem.Palette.Accent));
        return badges;
    }
}
