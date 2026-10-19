namespace Marvel.Client;

/// <summary>A library snapshot; its revision has no relationship to a game's revision.</summary>
public sealed record SavedDeck(string Id, int Revision, DeckDraft Draft);
