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
    {
        var strip = new PanelContainer
        {
            Name = "SeatStrip",
            ThemeTypeVariation = GodotThemeVariations.TabletopSeatStrip,
            CustomMinimumSize = new Vector2(300, 0),
        };
        var rail = new HBoxContainer { Name = "SeatSwitcher", ThemeTypeVariation = GodotThemeVariations.CompactRow };
        strip.AddChild(rail);
        foreach (PlayerSummaryDescriptor summary in board.PlayerSummaries.OrderBy(item => item.Seat))
        {
            BoardLanePresentation? lane = board.Lanes.FirstOrDefault(item => item.Seat == summary.Seat);
            int pendingEncounters = board.PendingEncounterCount(summary.Seat);
            var select = new Button
            {
                Name = $"SeatSwitch{summary.Seat}",
                Text = SeatLabel(lane, summary, compact: false, pendingEncounters)
                    + $"\n{RoleMarkers(summary.Seat, selection)}"
                    + (summary.Seat == selection.ExpandedSeat ? "\nInspecting this seat" : ""),
                Disabled = summary.Seat == selection.ExpandedSeat,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = false,
                AutowrapMode = TextServer.AutowrapMode.Word,
                TooltipText = $"{RoleMarkers(summary.Seat, selection)}. "
                    + "Show this public player workspace without changing the pending decision. "
                    + PendingEncounterIndicator.Description(pendingEncounters, summary.Seat),
            };
            select.Pressed += () => switchSeat(summary.Seat);
            rail.AddChild(select);
        }

        return strip;
    }

    /// <summary>The tabletop rail: one <see cref="SeatTile"/> per seat, its words in the tooltip.</summary>
    internal static PanelContainer CreateCompact(
        BoardPresentation board,
        DisplayedSeatSelection selection,
        Action<int> switchSeat,
        InterfaceScale scale)
    {
        var strip = new PanelContainer { Name = "SeatStrip", ThemeTypeVariation = GodotThemeVariations.TabletopSeatStrip };
        var rail = new VBoxContainer { Name = "SeatSwitcher", ThemeTypeVariation = GodotThemeVariations.CompactRow };
        strip.AddChild(rail);
        foreach (PlayerSummaryDescriptor summary in board.PlayerSummaries.OrderBy(item => item.Seat))
        {
            int pending = board.PendingEncounterCount(summary.Seat);
            BoardLanePresentation? lane = board.Lanes.FirstOrDefault(item => item.Seat == summary.Seat);
            Button tile = SeatTile.Create(board, summary, selection, scale,
                $"{SeatLabel(lane, summary, compact: true, pending)}\n{RoleMarkers(summary.Seat, selection)}"
                + (summary.Seat == selection.ExpandedSeat ? "\nInspecting this seat." : "")
                + "\nShow this public player workspace without changing the pending decision. "
                + PendingEncounterIndicator.Description(pending, summary.Seat));
            tile.Pressed += () => switchSeat(summary.Seat);
            rail.AddChild(tile);
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
