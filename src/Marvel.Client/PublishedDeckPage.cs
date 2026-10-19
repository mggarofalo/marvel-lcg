namespace Marvel.Client;

/// <summary>A public result page and the site's available filter choices.</summary>
public sealed record PublishedDeckPage(IReadOnlyList<PublishedDeck> Decks,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Filters,
    IReadOnlyDictionary<string, string> Packs, bool HasNext);
