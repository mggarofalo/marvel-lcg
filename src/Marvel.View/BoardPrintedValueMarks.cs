namespace Marvel.View;

/// <summary>Copies canonical printed facts without interpreting rendered text.</summary>
public static class BoardPrintedValueMarks
{
    /// <summary>Projects authorized printed values without consulting gameplay state.</summary>
    public static IReadOnlyList<BoardPrintedValueMark> From(
        IReadOnlyDictionary<string, CardPrintedValue> printed) =>
        [.. printed.Select(value => new BoardPrintedValueMark(
            value.Key, value.Value.PerPlayer, value.Value.ConsequentialDamage)
        {
            Value = value.Value.Value,
            SpecialStar = value.Value.SpecialStar,
        })];
}
