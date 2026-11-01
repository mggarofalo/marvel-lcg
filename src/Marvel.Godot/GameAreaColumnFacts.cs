using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The typed facts that mark one game-area column for the inspected seat, and the sentences explaining them.</summary>
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

    private bool Acting => board.Kang?.Acting?.GameArea == area.Id;

    private bool OutOfReach => placed.Any(reach.Contains);

    /// <summary>The column's markers, in display order.</summary>
    internal GameAreaMarker[] Markers()
    {
        var markers = new List<GameAreaMarker>();
        if (area.PlayAreas.Contains(seat)) markers.Add(GameAreaMarker.Yours);
        if (Acting) markers.Add(GameAreaMarker.Acting);
        if (area.Retained) markers.Add(GameAreaMarker.Retained);
        if (departures.Length > 0) markers.Add(GameAreaMarker.Departing);
        if (OutOfReach) markers.Add(GameAreaMarker.OutOfReach);
        return [.. markers];
    }

    /// <summary>
    /// The engine sentences behind the markers, most urgent first: why the area is out of reach, what is
    /// acting and who departs, then the notes anchored to this area's schemes.
    /// </summary>
    internal string[] Details()
    {
        var details = new List<string>();
        if (OutOfReach) details.Add(OutOfReachReason());
        if (Acting) details.Add(board.Kang!.Acting!.Summary);
        details.AddRange(departures.Select(departure => departure.Summary));
        details.AddRange(GameAreaTableProjection.AnchoredNotes(board.Kang?.Notes ?? [], area.MainSchemes));
        return [.. details];
    }

    private string OutOfReachReason()
    {
        string name = board.Players.FirstOrDefault(item => item.Seat == seat)?.Name ?? $"Player {seat + 1}";
        string? isolation = board.Kang?.Notes.FirstOrDefault(note => note.Kind == KangNoteKind.AreaIsolation)?.Text;
        return isolation is null ? $"Out of reach for {name}." : $"Out of reach for {name}. {isolation}";
    }
}
