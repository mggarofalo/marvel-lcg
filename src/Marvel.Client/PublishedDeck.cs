namespace Marvel.Client;

/// <summary>One public search result, whose URL can be passed to the bounded deck importer.</summary>
public sealed record PublishedDeck(string Name, string Url, string Hero, string Author);
