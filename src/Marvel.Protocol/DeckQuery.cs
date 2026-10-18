namespace Marvel.Protocol;

/// <summary>A read-only assessment of an editable, possibly invalid draft.</summary>
public sealed record DeckQuery(string Hero, IReadOnlyList<string> Cards)
{
    /// <summary>Requests a bounded research page instead of assessment; Hero and Cards must be empty.</summary>
    public int? CatalogPage { get; init; }
}
