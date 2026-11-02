using Marvel.View;

namespace Marvel.Godot;

/// <summary>The facts one game-area column states for the inspected seat, and the tooltip that explains them.</summary>
/// <param name="board">The published board.</param>
/// <param name="area">The game area the column shows.</param>
/// <param name="seat">The inspected seat.</param>
/// <param name="placed">Every card the column places: villains, main schemes, side schemes and environments.</param>
/// <param name="reach">The cards the inspected seat's cards cannot affect.</param>
internal sealed class GameAreaColumnFacts(BoardPresentation board, GameAreaDescriptor area, int seat,
    IReadOnlyList<int> placed, IReadOnlySet<int> reach)
{
    private readonly KangDepartureDescriptor[] departures =
        [.. (board.Kang?.Departures ?? []).Where(item => item.SourceGameArea == area.Id)];

    private VillainPhaseFocusDescriptor? Acting => board.Kang?.Acting is { } acting && acting.GameArea == area.Id ? acting : null;

    private bool OutOfReach => placed.Any(reach.Contains);

    // A leaving group's candidate destinations are lit for as long as its departure is pending,
    // which spans the join prompt that chooses among them.
    private bool Destination => (board.Kang?.Departures ?? []).Any(item => item.Destinations.Contains(area.Id));

    /// <summary>The step resolving here, if this area is acting.</summary>
    internal int? ActingStep => Acting?.Step;

    /// <summary>The column's facts, in display order.</summary>
    internal TableFact[] Facts()
    {
        var facts = new List<TableFact>();
        if (area.PlayAreas.Contains(seat)) facts.Add(TableFact.YourArea);
        if (Acting is not null) facts.Add(TableFact.Acting);
        if (area.Retained) facts.Add(TableFact.Retained);
        if (departures.Length > 0) facts.Add(TableFact.Departing);
        if (Destination) facts.Add(TableFact.Destination);
        if (OutOfReach) facts.Add(TableFact.OutOfReach);
        return [.. facts];
    }

    /// <summary>
    /// The full explanation, most urgent first: why the area is out of reach, that it is retained, what is
    /// acting and who departs, then the rules anchored to this area's schemes.
    /// </summary>
    internal string Tooltip(string title)
    {
        var lines = new List<string> { title };
        if (OutOfReach) lines.Add(TableWording.For(TableWording.Fact(Reason())).Tooltip);
        if (area.Retained) lines.Add(TableWording.For(TableFact.Retained).Tooltip);
        if (Acting is { } acting)
            lines.Add($"{TableWording.For(TableWording.Step(acting.Step)).Tooltip} ({GameAreaTableProjection.Names(board, [acting.Seat])[0]})");
        lines.AddRange(departures.Select(Departure));
        if (Destination) lines.Add(TableWording.For(TableFact.Destination).Tooltip);
        lines.AddRange(GameAreaTableProjection.AnchoredRules(board, area.MainSchemes));
        return string.Join("\n", lines);
    }

    // Client validation admits out-of-reach cards only with their reason.
    private ReachReason Reason() => board.Players.FirstOrDefault(item => item.Seat == seat)?.OutOfReachReason
        ?? throw new InvalidOperationException($"seat {seat} has out-of-reach cards without a published reason");

    private string Departure(KangDepartureDescriptor departure) => TableWording.Departure(
        GameAreaTableProjection.Title(board, departure.Villain), GameAreaTableProjection.Names(board, departure.Seats),
        departure.CompletedScheme is not null, departure.Reunion);
}
