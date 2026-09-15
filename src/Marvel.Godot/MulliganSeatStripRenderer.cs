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
        var rail = new HBoxContainer
        {
            Name = "SeatSwitcher",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        strip.AddChild(rail);
        foreach (PlayerSummaryDescriptor summary in board.PlayerSummaries.OrderBy(item => item.Seat))
        {
            BoardLanePresentation? lane = board.Lanes.FirstOrDefault(item => item.Seat == summary.Seat);
            var select = new Button
            {
                Name = $"SeatSwitch{summary.Seat}",
                Text = SeatLabel(lane, summary, summary.Seat == selection.ExpandedSeat),
                Disabled = summary.Seat == selection.ExpandedSeat,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                TooltipText = $"{RoleMarkers(summary.Seat, selection)}. "
                    + "Show this public player workspace without changing the pending decision.",
            };
            select.Pressed += () => switchSeat(summary.Seat);
            rail.AddChild(select);
        }

        return strip;
    }

    private static string SeatLabel(
        BoardLanePresentation? lane,
        PlayerSummaryDescriptor seat,
        bool expanded)
    {
        string title = lane?.Title ?? $"PLAYER {seat.Seat + 1}";
        string health = seat.Health is { } current ? $"HP {current}" : "HP —";
        string form = string.IsNullOrWhiteSpace(seat.Form) ? string.Empty : $" · {seat.Form.ToUpperInvariant()}";
        string statuses = seat.Statuses.Count == 0
            ? string.Empty
            : $" · {string.Join(" / ", seat.Statuses).ToUpperInvariant()}";
        return $"{(expanded ? "✓ " : string.Empty)}{title} · {health}{form}{statuses}";
    }

    internal static string RoleMarkers(int seat, DisplayedSeatSelection selection)
    {
        string[] roles =
        [
            selection.ActivePlayer == seat ? "TURN" : string.Empty,
            selection.PromptOwner == seat ? "DECISION" : string.Empty,
            selection.ViewedPrivateSeat == seat ? "PRIVATE VIEW" : string.Empty,
            selection.PublicFocusSeat == seat ? "PUBLIC FOCUS" : string.Empty,
        ];
        return string.Join("  ·  ", roles.Where(role => role.Length > 0).DefaultIfEmpty("OBSERVING"));
    }

}
