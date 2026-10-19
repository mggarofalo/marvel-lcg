namespace Marvel.Protocol;

/// <summary>The host's admitted public player-card pool and identity choices.</summary>
public sealed record DeckCatalog(IReadOnlyList<DeckHero> Heroes, IReadOnlyList<DeckCard> Cards);
