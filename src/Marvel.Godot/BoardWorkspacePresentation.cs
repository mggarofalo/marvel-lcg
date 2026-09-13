using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A fixed scenario and one selected player's visibility-safe workspace.</summary>
public sealed record BoardWorkspacePresentation(
    IReadOnlyList<BoardLanePresentation> Scenario,
    IReadOnlyList<BoardSeatPresentation> Seats,
    BoardSeatPresentation? ViewedSeat)
{
    /// <summary>Builds the tabletop solely from the authorized board and prompt mappings.</summary>
    public static BoardWorkspacePresentation From(
        BoardPresentation board,
        Prompt? prompt,
        int? selectedAffordance,
        IReadOnlyList<int> selectedTargets,
        int? viewedSeat)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(selectedTargets);

        BoardLanePresentation[] playerLanes =
            [.. board.Lanes.Where(lane => lane.Seat is not null).OrderBy(lane => lane.Seat)];
        int? chosen = ChooseSeat(playerLanes, prompt, viewedSeat);
        IReadOnlySet<int> legalTargets = LegalTargets(prompt, selectedAffordance);
        var selected = selectedTargets.ToHashSet();
        BoardSeatPresentation[] seats = [.. playerLanes.Select(lane => PresentSeat(
            lane, board, prompt, legalTargets, selected, chosen))];
        return new BoardWorkspacePresentation(
            [.. board.Lanes.Where(lane => lane.Seat is null)],
            seats,
            seats.FirstOrDefault(seat => seat.IsViewed));
    }

    private static int? ChooseSeat(
        BoardLanePresentation[] seats, Prompt? prompt, int? viewedSeat)
    {
        if (viewedSeat is { } remembered && seats.Any(lane => lane.Seat == remembered))
        {
            return remembered;
        }
        if (prompt is not null && seats.Any(lane => lane.Seat == prompt.Player))
        {
            return prompt.Player;
        }
        BoardLanePresentation? readable = seats.FirstOrDefault(lane => lane.Areas
            .Where(area => area.Zone == "HandsArea")
            .SelectMany(area => area.Cards)
            .Any(card => !card.Concealed));
        if (readable is not null)
        {
            return readable.Seat;
        }
        return seats.Length == 0 ? null : seats[0].Seat;
    }

    private static HashSet<int> LegalTargets(Prompt? prompt, int? selectedAffordance)
    {
        if (prompt is null)
        {
            return new HashSet<int>();
        }

        IEnumerable<Affordance> choices = selectedAffordance is { } id
            ? prompt.Affordances.Where(option => option.Id == id)
            : prompt.Affordances;
        return choices
            .SelectMany(option => option.Targets?.Legal ?? [])
            .ToHashSet();
    }

    private static BoardSeatPresentation PresentSeat(
        BoardLanePresentation lane,
        BoardPresentation board,
        Prompt? prompt,
        IReadOnlySet<int> legalTargets,
        IReadOnlySet<int> selectedTargets,
        int? viewedSeat)
    {
        int seat = lane.Seat!.Value;
        BoardAreaPresentation? hand = board.Areas.FirstOrDefault(area =>
            area.Zone == "HandsArea" && area.Seat == seat);
        BoardCardPresentation? identity = lane.Areas
            .Where(area => area.Zone == "HeroArea")
            .SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.Kind is "HERO" or "ALTER EGO");
        HashSet<int> ids = VisibleTargetIds(lane, hand);

        int actions = prompt?.Affordances.Count(option =>
            option.IsLegal && option.AnchorPlayer == seat) ?? 0;
        int targets = ids.Count(legalTargets.Contains);
        int selected = ids.Count(selectedTargets.Contains);
        string summary = IdentitySummary(identity);
        bool firstPlayer = identity?.Fields.Any(field =>
            field.Name == "FIRST PLAYER TOKEN" && field.Value != "0") == true;
        return new BoardSeatPresentation(
            seat,
            lane.Title,
            summary,
            IsViewed: seat == viewedSeat,
            IsAnswering: prompt?.Player == seat,
            IsFirstPlayer: firstPlayer,
            ActionCount: actions,
            LegalTargetCount: targets,
            SelectedTargetCount: selected,
            Lane: lane,
            Hand: hand);
    }

    private static HashSet<int> VisibleTargetIds(
        BoardLanePresentation lane, BoardAreaPresentation? hand)
    {
        HashSet<int> ids = lane.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .Select(card => card.TargetId)
            .OfType<int>()
            .ToHashSet();
        ids.UnionWith(hand?.Cards.Select(card => card.TargetId).OfType<int>() ?? []);
        return ids;
    }

    private static string IdentitySummary(BoardCardPresentation? identity)
    {
        if (identity is null)
        {
            return "No visible identity";
        }

        string health = identity.Fields.FirstOrDefault(field => field.Name == "HEALTH")?.Value
            ?? string.Empty;
        string state = identity.Status == "READY" ? string.Empty : identity.Status;
        return string.Join(" · ", new[] { identity.Title, identity.Kind, Health(health), state }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string Health(string value) =>
        value.Length == 0 ? string.Empty : $"HP {value}";
}
