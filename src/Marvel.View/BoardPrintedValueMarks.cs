using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Describes authorized print annotations through the dataset notation owner.</summary>
public static class BoardPrintedValueMarks
{
    /// <summary>Projects marks without evaluating quantities or consulting game state.</summary>
    public static IReadOnlyList<BoardPrintedValueMark> From(
        CardKind kind, IReadOnlyDictionary<string, string> printed) =>
        [.. printed.Where(value => value.Value.Contains('*')).Select(value =>
            new BoardPrintedValueMark(value.Key,
                PrintedAttributeNotation.IsPerPlayer(kind, value.Key, value.Value),
                PrintedAttributeNotation.ConsequentialDamage(kind, value.Key, value.Value)))];
}
