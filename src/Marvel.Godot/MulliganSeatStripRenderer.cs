using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders public co-operative seat summaries and their workspace switcher.</summary>
internal static class MulliganSeatStripRenderer
{
    internal static PanelContainer Create(
        BoardPresentation board,
        int expandedPlayer,
        Action<int> switchSeat)
    {
        var strip = new PanelContainer
        {
            Name = "SeatStrip",
            ThemeTypeVariation = GodotThemeVariations.TabletopSeatStrip,
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
            var seat = new VBoxContainer
            {
                Name = $"SeatSummary{summary.Seat}",
                ThemeTypeVariation = GodotThemeVariations.TightStack,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            seat.AddThemeConstantOverride("separation", 0);
            seat.AddChild(Label(lane?.Title ?? $"PLAYER {summary.Seat + 1}", GodotThemeVariations.Eyebrow));
            seat.AddChild(Label(Summary(board, summary), GodotThemeVariations.Caption, wrap: true));
            var select = new Button
            {
                Name = $"SeatSwitch{summary.Seat}",
                Text = summary.Seat == expandedPlayer ? "✓ EXPANDED" : "View public area",
                Disabled = summary.Seat == expandedPlayer,
                TooltipText = "Expand this public player workspace without changing the pending decision.",
            };
            select.Pressed += () => switchSeat(summary.Seat);
            seat.AddChild(select);
            rail.AddChild(seat);
        }

        return strip;
    }

    private static string Summary(BoardPresentation board, PlayerSummaryDescriptor seat)
    {
        string identity = CardTitle(board, seat.Identity) ?? "Identity not visible";
        string form = string.IsNullOrWhiteSpace(seat.Form) ? "form unavailable" : seat.Form.ToUpperInvariant();
        string health = seat.Health is { } current ? $"HP {current}" : "HP unavailable";
        string statuses = seat.Statuses.Count == 0 ? "no statuses" : string.Join(", ", seat.Statuses);
        string enemies = Named(board, seat.EngagedEnemies, "no engaged enemies");
        string defenders = Named(board, seat.OfferedDefenders, "no offered defenders");
        return $"{identity}  ·  {form}  ·  {health}  ·  STATUS {statuses}"
            + $"  ·  ENGAGED {enemies}  ·  DEFENDERS {defenders}";
    }

    private static string Named(BoardPresentation board, IReadOnlyList<int> ids, string empty) => ids.Count == 0
        ? empty
        : string.Join(", ", ids.Select(id => CardTitle(board, id) ?? $"CARD {id}"));

    private static string? CardTitle(BoardPresentation board, int? id) => id is null ? null
        : board.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(card => card.TargetId == id)?.Title;

    private static Label Label(string text, string variation, bool wrap = false) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
    };
}
