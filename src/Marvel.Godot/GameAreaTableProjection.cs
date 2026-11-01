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
        int players = board.Kang?.StartingPlayers ?? 0;
        return new GameAreaTable(board.Kang is { } kang ? TableWording.Progress(kang) : string.Empty,
            notes.Any(note => note.Kind == KangNoteKind.SharedEncounterDeck)
                ? TableWording.Rule(KangNoteKind.SharedEncounterDeck, players) : null,
            [.. notes.Where(note => note.Card is null).Select(note => TableWording.Rule(note.Kind, players))], columns, reach);
    }

    private static IReadOnlyList<KangNoteDescriptor> Notes(BoardPresentation board) => board.Kang?.Notes ?? [];

    private static GameAreaColumn Central(BoardPresentation board, IReadOnlyList<KangNoteDescriptor> notes)
    {
        int[] schemes = [.. board.Kang?.CentralSchemes ?? []];
        return new GameAreaColumn(null, TableWording.CentralTitle, [], AnchoredNotes(board, schemes),
            [.. board.GameAreas.Where(area => area.Central).SelectMany(area => area.Villains)],
            schemes, Encounter(board, null));
    }

    private static GameAreaColumn Column(BoardPresentation board, GameAreaDescriptor area, int seat,
        IReadOnlySet<int> reach)
    {
        int[] encounter = Encounter(board, area.Id);
        var facts = new GameAreaColumnFacts(board, area, seat, [.. area.Villains, .. area.MainSchemes, .. encounter], reach);
        string title = TableWording.AreaTitle(Names(board, area.Retained ? area.FormerSeats : area.PlayAreas), area.Retained);
        return new GameAreaColumn(area.Id, title, facts.Markers(), facts.Details(), area.Villains,
            area.MainSchemes, encounter);
    }

    internal static string[] AnchoredNotes(BoardPresentation board, IReadOnlyList<int> cards) =>
        [.. Notes(board).Where(note => note.Card is { } card && cards.Contains(card))
            .Select(note => TableWording.Rule(note.Kind, board.Kang?.StartingPlayers ?? 0))];

    /// <summary>The published names of the given seats, in seat order; the villain's play area has none.</summary>
    internal static string[] Names(BoardPresentation board, IReadOnlyList<int> seats) =>
        [.. seats.Where(seat => seat >= 0).Select(seat => board.Players.FirstOrDefault(item => item.Seat == seat)?.Name
            ?? $"Player {seat + 1}")];

    /// <summary>A visible card's title, or a neutral name when the card is not on the board.</summary>
    internal static string Title(BoardPresentation board, int card) => board.Areas.SelectMany(SpatialTableZones.Current)
        .FirstOrDefault(item => item.TargetId == card)?.Title ?? "Kang (II)";

    private static int[] Encounter(BoardPresentation board, int? area) => [.. board.Areas
        .Where(SpatialTableZones.IsEncounterSupport)
        .SelectMany(SpatialTableZones.Current)
        .Where(card => card.TargetId is not null && card.GameArea == area)
        .Select(card => card.TargetId!.Value)
        .Distinct()];
}
