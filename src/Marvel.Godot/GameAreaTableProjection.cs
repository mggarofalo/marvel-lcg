using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Arranges the published game-area and Kang facts into table columns for one inspected seat.</summary>
/// <remarks>
/// Every membership, marker and sentence comes from a typed descriptor field: area ids, seats,
/// villains, schemes, each card's game area, the acting focus, departures, notes addressed by
/// <see cref="KangNoteKind"/> and the seat's out-of-reach ids. Nothing is inferred from card text.
/// </remarks>
internal static class GameAreaTableProjection
{
    /// <summary>The table's game-area presentation, or null when the game has neither Kang facts nor a split.</summary>
    internal static GameAreaTable? From(BoardPresentation board, int seat)
    {
        ArgumentNullException.ThrowIfNull(board);
        bool separated = board.GameAreas.Any(area => !area.Central);
        if (board.Kang is null && !separated) return null;
        IReadOnlyList<KangNoteDescriptor> notes = Notes(board);
        HashSet<int> reach = [.. board.Players.FirstOrDefault(item => item.Seat == seat)?.OutOfReach ?? []];
        GameAreaColumn[] columns = separated
            ? [Central(board, notes), .. board.GameAreas.Where(area => !area.Central)
                .Select(area => Column(board, area, seat, reach))]
            : [];
        return new GameAreaTable(Progress(board.Kang), board.Kang?.Acting?.Summary,
            notes.FirstOrDefault(note => note.Kind == KangNoteKind.SharedEncounterDeck)?.Text,
            [.. notes.Where(note => note.Card is null).Select(note => note.Text)], columns, reach);
    }

    private static IReadOnlyList<KangNoteDescriptor> Notes(BoardPresentation board) => board.Kang?.Notes ?? [];

    private static string Progress(KangTableDescriptor? kang) => kang switch
    {
        null => string.Empty,
        { StoredDominions: 1 } => $"{kang.Progress} 1 Kang's Dominion stored under stage 4A.",
        { StoredDominions: > 1 } => $"{kang.Progress} {kang.StoredDominions} Kang's Dominions stored under stage 4A.",
        _ => kang.Progress,
    };

    private static GameAreaColumn Central(BoardPresentation board, IReadOnlyList<KangNoteDescriptor> notes)
    {
        int[] schemes = [.. board.Kang?.CentralSchemes ?? []];
        return new GameAreaColumn(null, "CENTRAL", [], AnchoredNotes(notes, schemes),
            [.. board.GameAreas.Where(area => area.Central).SelectMany(area => area.Villains)],
            schemes, Encounter(board, null));
    }

    private static GameAreaColumn Column(BoardPresentation board, GameAreaDescriptor area, int seat,
        IReadOnlySet<int> reach)
    {
        var facts = new GameAreaColumnFacts(board, area, seat, reach);
        return new GameAreaColumn(area.Id, area.Label, facts.Markers(), facts.Details(), area.Villains,
            area.MainSchemes, Encounter(board, area.Id));
    }

    internal static string[] AnchoredNotes(IReadOnlyList<KangNoteDescriptor> notes, IReadOnlyList<int> cards) =>
        [.. notes.Where(note => note.Card is { } card && cards.Contains(card)).Select(note => note.Text)];

    private static int[] Encounter(BoardPresentation board, int? area) => [.. board.Areas
        .Where(SpatialTableZones.IsEncounterSupport)
        .SelectMany(SpatialTableZones.Current)
        .Where(card => card.TargetId is not null && card.GameArea == area)
        .Select(card => card.TargetId!.Value)
        .Distinct()];
}
