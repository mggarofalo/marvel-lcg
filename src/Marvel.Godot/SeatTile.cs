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

    /// <summary>The seat's identity card title as published, or the seat's number when no identity is on the board.</summary>
    internal static string Hero(BoardPresentation board, PlayerSummaryDescriptor summary) => summary.Identity is { } id
        && board.Areas.SelectMany(SpatialTableZones.Current).FirstOrDefault(card => card.TargetId == id) is { Title.Length: > 0 } card
            ? card.Title : TableWording.Seat(summary.Seat);

    /// <summary>The tile, as the seat's workspace switch.</summary>
    internal static Button Create(BoardPresentation board, PlayerSummaryDescriptor summary, DisplayedSeatSelection selection,
        InterfaceScale scale, string tooltip)
    {
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        string hero = Hero(board, summary);
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
        facts.AddChild(TableBlockStyle.Line("Hero", hero.ToUpperInvariant(), scale, VisualSystem.Palette.Text));
        facts.AddChild(SeatHealth.Create(summary.Health, SeatHealth.Maximum(board, summary.Identity), scale));
        row.AddChild(facts);
        body.AddChild(row);
        body.AddChild(Badges(board, summary, roles, scale));
        tile.AddChild(body);
        // Badges wrap on a narrow rail; the tile grows to hold every row rather than clip one.
        body.MinimumSizeChanged += () => tile.CustomMinimumSize = new Vector2(0,
            Math.Max(Height(scale), body.GetCombinedMinimumSize().Y + 2 * spacing.ExtraSmall));
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

    // The seat's roles, then its form, statuses, engaged enemies and pending encounters. The name line
    // holds only the name, so a narrow rail still shows it whole. Who must answer is a labelled chip in
    // the accent, beside the portrait's accent edge; the other roles are icons, each named on hover.
    private static HFlowContainer Badges(BoardPresentation board, PlayerSummaryDescriptor summary, TableFact[] roles,
        InterfaceScale scale)
    {
        var badges = new HFlowContainer { Name = "Badges", MouseFilter = Control.MouseFilterEnum.Ignore };
        badges.AddThemeConstantOverride("h_separation", VisualSystem.Spacing(scale).ExtraSmall);
        int icon = TableBlockStyle.Icon(scale);
        foreach (TableFact role in roles)
            badges.AddChild(role == TableFact.AnsweringSeat ? Answering(scale) : Role(role, icon));
        if (Form(summary.Form) is { } form) badges.AddChild(Chip.Create("Form", TableWording.For(form), null, scale));
        foreach (var status in summary.Statuses.GroupBy(value => value, StringComparer.OrdinalIgnoreCase))
            badges.AddChild(StatusBadge.Supports(status.Key)
                ? StatusBadge.Create(status.Key, status.Key, status.Count(), compact: true, scale)
                : Chip.Create("Status" + status.Key, new Wording("", status.Key, status.Key),
                    status.Count() > 1 ? status.Count() : null, scale));
        if (summary.EngagedEnemies.Count > 0)
            badges.AddChild(Chip.Create("Engaged", TableWording.For(TableFact.Engaged), summary.EngagedEnemies.Count, scale));
        int pending = board.PendingEncounterCount(summary.Seat);
        if (pending > 0) badges.AddChild(TableBlockStyle.Whole("PendingEncounters", PendingEncounterIndicator.Compact(pending).TrimStart(' ', '·'),
            scale, VisualSystem.Palette.Accent));
        return badges;
    }

    // An icon that passes the pointer on, so it shows its own tooltip and the tile still takes the press.
    private static TextureRect Role(TableFact role, int icon)
    {
        Wording wording = TableWording.For(role);
        TextureRect mark = TableIcon.Create(wording.Icon, icon, ClientTheme.ToGodot(VisualSystem.Palette.Accent));
        mark.Name = "Role" + role;
        mark.AccessibilityName = wording.Label;
        mark.TooltipText = wording.Tooltip;
        mark.MouseFilter = Control.MouseFilterEnum.Pass;
        mark.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        return mark;
    }

    private static PanelContainer Answering(InterfaceScale scale)
    {
        PanelContainer chip = Chip.Create("Role" + TableFact.AnsweringSeat, TableWording.For(TableFact.AnsweringSeat), null, scale);
        using StyleBoxFlat style = TableBlockStyle.Surface(scale, VisualSystem.Palette.Accent, 2);
        chip.AddThemeStyleboxOverride("panel", style);
        return chip;
    }
}
