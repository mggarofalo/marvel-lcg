namespace Marvel.View;

/// <summary>Meaning of a printed numeric annotation, independent of typography.</summary>
public sealed record BoardPrintedValueMark(string Attribute, bool PerPlayer, int ConsequentialDamage)
{
    /// <summary>The printed numeral or symbol, without a mark suffix.</summary>
    public string Value { get; init; } = "";

    /// <summary>A printed reminder to consult the corresponding mandatory ability.</summary>
    public bool SpecialStar { get; init; }
}
