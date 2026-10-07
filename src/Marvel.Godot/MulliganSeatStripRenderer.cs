using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders public co-operative seat summaries and their workspace switcher.</summary>
internal static class MulliganSeatStripRenderer
{
    internal static PanelContainer Create(
        BoardPresentation board,
        int expandedPlayer,
        Action<int> switchSeat) => Create(board, new DisplayedSeatSelection(
            expandedPlayer, null, null, null, null), switchSeat);

    internal static PanelContainer Create(
        BoardPresentation board,
        DisplayedSeatSelection selection,
        Action<int> switchSeat)
        => Create(board, selection, switchSeat, compact: false);

    internal static PanelContainer CreateCompact(
        BoardPresentation board,
        DisplayedSeatSelection selection,
        Action<int> switchSeat)
        => Create(board, selection, switchSeat, compact: true);

    private static PanelContainer Create(
        BoardPresentation board,
        DisplayedSeatSelection selection,
        Action<int> switchSeat,
        bool compact)
    {
        var strip = new PanelContainer
        {
            Name = "SeatStrip",
            ThemeTypeVariation = GodotThemeVariations.TabletopSeatStrip,
            CustomMinimumSize = new Vector2(compact ? 0 : 300, 0),
        };
        BoxContainer rail = compact ? new VBoxContainer() : new HBoxContainer();
        rail.Name = "SeatSwitcher";
        rail.ThemeTypeVariation = GodotThemeVariations.CompactRow;
        strip.AddChild(rail);
        foreach (PlayerSummaryDescriptor summary in board.PlayerSummaries.OrderBy(item => item.Seat))
        {
            BoardLanePresentation? lane = board.Lanes.FirstOrDefault(item => item.Seat == summary.Seat);
            int pendingEncounters = board.PendingEncounterCount(summary.Seat);
            var select = new Button
            {
                Name = $"SeatSwitch{summary.Seat}",
                Text = SeatLabel(lane, summary, compact, pendingEncounters)
                    + $"\n{RoleMarkers(summary.Seat, selection)}"
                    + (summary.Seat == selection.ExpandedSeat ? "\nInspecting this seat" : ""),
                Disabled = summary.Seat == selection.ExpandedSeat,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = compact ? new Vector2(0, 100) : Vector2.Zero,
                ClipText = false,
                AutowrapMode = TextServer.AutowrapMode.Word,
                TooltipText = $"{RoleMarkers(summary.Seat, selection)}. "
                    + "Show this public player workspace without changing the pending decision. "
                    + PendingEncounterIndicator.Description(pendingEncounters, summary.Seat),
            };
            if (compact)
            {
                select.AddThemeFontSizeOverride("font_size", 16);
            }
            select.Pressed += () => switchSeat(summary.Seat);
            rail.AddChild(select);
        }

        return strip;
    }

    private static string SeatLabel(
        BoardLanePresentation? lane,
        PlayerSummaryDescriptor seat,
        bool compact, int pendingEncounters)
    {
        string title = lane?.Title ?? $"PLAYER {seat.Seat + 1}";
        string health = (seat.Health is { } current ? $"HP {current}" : "HP —")
            + PendingEncounterIndicator.Compact(pendingEncounters);
        string form = string.IsNullOrWhiteSpace(seat.Form) ? string.Empty : $" · {seat.Form.ToUpperInvariant()}";
        string statuses = seat.Statuses.Count == 0
            ? string.Empty
            : $" · {string.Join(" / ", seat.Statuses).ToUpperInvariant()}";
        string publicContext = (seat.EngagedEnemies.Count, seat.OfferedDefenders.Count) switch
        {
            (0, 0) => string.Empty,
            (var enemies, 0) => $"\nENGAGED {enemies}",
            (0, var defenders) => $"\nDEFENDERS {defenders}",
            (var enemies, var defenders) => $"\nENGAGED {enemies} · DEFENDERS {defenders}",
        };
        string[] titleParts = title.Split(" · ", StringSplitOptions.RemoveEmptyEntries);
        string compactTitle = titleParts.Length > 1
            ? $"P{seat.Seat + 1} · {titleParts[^1]}"
            : $"P{seat.Seat + 1} · {title}";
        return compact
            ? $"{compactTitle}\n{health}{form}{statuses}{publicContext}"
            : $"{title} · {health}{form}{statuses}";
    }

    internal static string RoleMarkers(int seat, DisplayedSeatSelection selection)
    {
        string[] roles =
        [
            selection.ActivePlayer == seat ? "Active" : string.Empty,
            selection.PromptOwner == seat ? "Answering" : string.Empty,
            selection.ViewedPrivateSeat == seat ? "Your seat" : string.Empty,
            selection.PublicFocusSeat == seat ? "Public focus" : string.Empty,
        ];
        return string.Join("  ·  ", roles.Where(role => role.Length > 0).DefaultIfEmpty("Observing"));
    }

}
