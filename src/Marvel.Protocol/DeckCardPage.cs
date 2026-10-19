namespace Marvel.Protocol;

/// <summary>A bounded public research catalog page; browsing does not grant runtime admission.</summary>
public sealed record DeckCardPage(IReadOnlyList<DeckCard> Cards, int? NextPage);
