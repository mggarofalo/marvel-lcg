namespace Marvel.Protocol;

/// <summary>A read-only assessment of an editable, possibly invalid draft.</summary>
public sealed record DeckQuery(string Hero, IReadOnlyList<string> Cards)
{
    /// <summary>Requests a bounded research page instead of assessment; Hero and Cards must be empty.</summary>
    public int? CatalogPage { get; init; }
    /// <summary>Includes host-checked row quantities for this exact draft.</summary>
    public bool IncludeQuantities { get; init; }
}
