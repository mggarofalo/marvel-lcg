using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds the fixed scenario and selected-player desktop workspace.</summary>
public static class BoardRenderer
{
    /// <summary>Replaces the visible board without changing the current decision draft.</summary>
    public static BoardRenderResult Render(BoardRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        VBoxContainer destination = request.Destination;
        BoardPresentation board = request.Board;
        HFlowContainer hand = request.Hand;
        Label handHeading = request.HandHeading;
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(hand);
        ArgumentNullException.ThrowIfNull(handHeading);
        ArgumentNullException.ThrowIfNull(request.ExpandedAreas);
        ArgumentNullException.ThrowIfNull(request.Pages);
        ArgumentNullException.ThrowIfNull(request.SelectedTargets);
        Clear(destination);
        Clear(hand);

        var result = new BoardRenderResult();
        InterfaceScale tableScale = VisualSystem.TabletopScale(request.Scale);
        var interaction = new BoardInteractionPresentation(
            request.Prompt, request.SelectedAffordance, request.SelectedTargets);
        var context = new BoardRenderContext(
            result, tableScale, request.ExpandedAreas, request.Pages, interaction, request.Art);
        BoardWorkspacePresentation workspace = BoardWorkspacePresentation.From(
            board, request.Prompt, request.SelectedAffordance,
            request.SelectedTargets, request.ViewedSeat);
        result.ViewedSeat = workspace.ViewedSeat?.Seat;

        foreach (BoardLanePresentation lane in workspace.Scenario)
        {
            destination.AddChild(BoardLaneRenderer.Render(lane, context));
        }
        AddSeatSwitcher(destination, workspace.Seats, result, tableScale);
        if (workspace.ViewedSeat is { } selected)
        {
            destination.AddChild(BoardLaneRenderer.Render(selected.Lane, context));
            BoardHandRenderer.Render(selected, hand, handHeading, context);
        }
        else
        {
            handHeading.Text = "HAND  ·  NO VISIBLE SEAT";
            hand.AddChild(Text("No player workspace is available.", GodotThemeVariations.MutedText));
        }

        return result;
    }

    private static void AddSeatSwitcher(
        VBoxContainer destination,
        IReadOnlyList<BoardSeatPresentation> seats,
        BoardRenderResult result,
        InterfaceScale scale)
    {
        if (seats.Count < 2)
        {
            return;
        }

        var switcher = new HFlowContainer
        {
            Name = "SeatSwitcher",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        foreach (BoardSeatPresentation seat in seats)
        {
            var button = new Button
            {
                Name = $"Seat{seat.Seat}",
                Text = SeatText(seat),
                TooltipText = $"View {seat.Name}. This does not change authority or the draft.",
                ToggleMode = true,
                ButtonPressed = seat.IsViewed,
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(
                    Math.Max(260, VisualSystem.Controls(scale).MinimumButtonWidth),
                    VisualSystem.Controls(scale).MinimumHeight),
                ThemeTypeVariation = seat.IsViewed
                    ? GodotThemeVariations.SelectedTargetButton
                    : GodotThemeVariations.ChoiceButton,
            };
            result.RegisterSeat(seat.Seat, button);
            button.Pressed += () => result.RequestViewedSeat(seat.Seat, button);
            switcher.AddChild(button);
        }
        destination.AddChild(switcher);
    }

    private static string SeatText(BoardSeatPresentation seat)
    {
        var markers = new List<string>();
        if (seat.IsAnswering) markers.Add("ANSWERING");
        if (seat.IsFirstPlayer) markers.Add("FIRST PLAYER");
        if (seat.ActionCount > 0) markers.Add($"ACTIONS {seat.ActionCount}");
        if (seat.LegalTargetCount > 0) markers.Add($"TARGETS {seat.LegalTargetCount}");
        if (seat.SelectedTargetCount > 0) markers.Add($"SELECTED {seat.SelectedTargetCount}");
        string suffix = markers.Count == 0 ? string.Empty : "  ·  " + string.Join("  ·  ", markers);
        return $"{(seat.IsViewed ? "✓" : "◇")} {seat.Name}\n{seat.Summary}{suffix}";
    }

    private static Label Text(string text, string variation) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
    };

    private static void Clear(Node destination)
    {
        foreach (Node child in destination.GetChildren())
        {
            destination.RemoveChild(child);
            child.QueueFree();
        }
    }
}
