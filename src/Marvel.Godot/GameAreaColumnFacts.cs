using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The typed facts that mark one game-area column for the inspected seat, and the sentences explaining them.</summary>
internal sealed class GameAreaColumnFacts(BoardPresentation board, GameAreaDescriptor area, int seat, IReadOnlySet<int> reach)
{
    private readonly KangDepartureDescriptor[] departures =
        [.. (board.Kang?.Departures ?? []).Where(item => item.SourceGameArea == area.Id)];

    private bool Yours => area.PlayAreas.Contains(seat);

    private bool Acting => board.Kang?.Acting?.GameArea == area.Id;

    private bool OutOfReach => area.Villains.Concat(area.MainSchemes).Any(reach.Contains);

    /// <summary>The column's markers, in display order.</summary>
    internal GameAreaMarker[] Markers()
    {
        var markers = new List<GameAreaMarker>();
        if (Yours) markers.Add(GameAreaMarker.Yours);
        if (Acting) markers.Add(GameAreaMarker.Acting);
        if (area.Retained) markers.Add(GameAreaMarker.Retained);
        if (departures.Length > 0) markers.Add(GameAreaMarker.Departing);
        if (OutOfReach) markers.Add(GameAreaMarker.OutOfReach);
        return [.. markers];
    }

    /// <summary>The engine sentences behind the markers, then the notes anchored to this area's schemes.</summary>
    internal string[] Details()
    {
        var details = new List<string>();
        if (Acting) details.Add(board.Kang!.Acting!.Summary);
        details.AddRange(departures.Select(Departure));
        details.AddRange(GameAreaTableProjection.AnchoredNotes(board.Kang?.Notes ?? [], area.MainSchemes));
        if (OutOfReach) details.Add(OutOfReachReason());
        return [.. details];
    }

    private static string Departure(KangDepartureDescriptor departure) => departure.Reunion
        ? $"{departure.Summary} No game area remains for this group to join, so the table reunites at stage 4A."
        : departure.Summary;

    private string OutOfReachReason()
    {
        string name = board.Players.FirstOrDefault(item => item.Seat == seat)?.Name ?? $"Player {seat + 1}";
        string? isolation = board.Kang?.Notes.FirstOrDefault(note => note.Kind == KangNoteKind.AreaIsolation)?.Text;
        return isolation is null ? $"Out of reach for {name}." : $"Out of reach for {name}. {isolation}";
    }
}
