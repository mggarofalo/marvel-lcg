using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Arranges the published game-area and Kang facts into table columns for one inspected seat.</summary>
/// <remarks>
/// Every membership, fact and tooltip comes from a typed descriptor field: area ids, seats,
/// villains, schemes, each card's game area, the acting focus, departures, notes addressed by
/// <see cref="KangNoteKind"/> and the seat's out-of-reach ids and reason, worded by
/// <see cref="TableWording"/>. Nothing is inferred from card text. The central column sits between
/// the game areas because everything in it is shared; that placement is our choice.
/// </remarks>
internal static class GameAreaTableProjection
{
    /// <summary>The table's game-area presentation, or null when the game has neither Kang facts nor a split.</summary>
    internal static GameAreaTable? From(BoardPresentation board, int seat)
    {
        ArgumentNullException.ThrowIfNull(board);
        bool separated = board.GameAreas.Any(area => !area.Central);
        if (board.Kang is null && !separated) return null;
        HashSet<int> reach = [.. board.Players.FirstOrDefault(item => item.Seat == seat)?.OutOfReach ?? []];
        GameAreaColumn[] areas = separated
            ? [.. board.GameAreas.Where(area => !area.Central).Select(area => Column(board, area, seat, reach))]
            : [];
        GameAreaColumn[] columns = separated ? [.. areas[..Middle(areas.Length)], Central(board), .. areas[Middle(areas.Length)..]] : [];
        return new GameAreaTable(Tracker(board.Kang), Rules(board), columns, reach);
    }

    /// <summary>Where the central column goes among <paramref name="areas"/> game areas: the middle, rounding right.</summary>
    internal static int Middle(int areas) => (areas + 1) / 2;

    private static TrackerState Tracker(KangTableDescriptor? kang) => kang is null
        ? new TrackerState("", 0, 0, null, 0, "")
        : new TrackerState(TableWording.Stage(kang), kang.RemainingLocalKangs,
            kang.Stage == 2 ? kang.RemainingLocalKangs + kang.Departures.Count : 0,
            kang.StoredDominions > 0 ? TableWording.For(TableFact.StoredDominions) : null,
            kang.StoredDominions, TableWording.Progress(kang));

    // Each rule once, table-wide rules first, then the anchored ones in the engine's order.
    private static string[] Rules(BoardPresentation board) => [.. Notes(board)
        .OrderBy(note => note.Card is null ? 0 : 1).Select(note => note.Kind).Distinct()
        .Select(kind => TableWording.Rule(kind, board.Kang?.StartingPlayers ?? 0))];

    private static IReadOnlyList<KangNoteDescriptor> Notes(BoardPresentation board) => board.Kang?.Notes ?? [];

    private static GameAreaColumn Central(BoardPresentation board)
    {
        int[] schemes = [.. board.Kang?.CentralSchemes ?? []];
        string[] lines = [TableWording.CentralTitle, .. Notes(board).Where(note => note.Kind == KangNoteKind.SharedEncounterDeck)
            .Select(note => TableWording.Rule(note.Kind, 0)), .. AnchoredRules(board, schemes)];
        return new GameAreaColumn(null, TableWording.CentralTitle, [TableFact.Shared], string.Join("\n", lines), null,
            [.. board.GameAreas.Where(area => area.Central).SelectMany(area => area.Villains)],
            schemes, Encounter(board, null));
    }

    private static GameAreaColumn Column(BoardPresentation board, GameAreaDescriptor area, int seat,
        IReadOnlySet<int> reach)
    {
        int[] encounter = Encounter(board, area.Id);
        var facts = new GameAreaColumnFacts(board, area, seat, [.. area.Villains, .. area.MainSchemes, .. encounter], reach);
        string title = TableWording.AreaTitle(Names(board, area.Retained ? area.FormerSeats : area.PlayAreas), area.Retained);
        return new GameAreaColumn(area.Id, title, facts.Facts(), facts.Tooltip(title), facts.ActingStep, area.Villains,
            area.MainSchemes, encounter);
    }

    /// <summary>The worded rules anchored to the given schemes.</summary>
    internal static string[] AnchoredRules(BoardPresentation board, IReadOnlyList<int> cards) =>
        [.. Notes(board).Where(note => note.Card is { } card && cards.Contains(card))
            .Select(note => TableWording.Rule(note.Kind, board.Kang?.StartingPlayers ?? 0))];

    /// <summary>The published names of the given seats, in seat order; the villain's play area has none.</summary>
    internal static string[] Names(BoardPresentation board, IReadOnlyList<int> seats) =>
        [.. seats.Where(seat => seat >= 0).Select(seat => board.Players.FirstOrDefault(item => item.Seat == seat)?.Name
            ?? $"Player {seat + 1}")];

    /// <summary>A visible card's title, or the stage's name when the card is not on the board.</summary>
    internal static string Title(BoardPresentation board, int card) => board.Areas.SelectMany(SpatialTableZones.Current)
        .FirstOrDefault(item => item.TargetId == card)?.Title ?? "Kang (II)";

    private static int[] Encounter(BoardPresentation board, int? area) => [.. board.Areas
        .Where(SpatialTableZones.IsEncounterSupport)
        .SelectMany(SpatialTableZones.Current)
        .Where(card => card.TargetId is not null && card.GameArea == area)
        .Select(card => card.TargetId!.Value)
        .Distinct()];
}
