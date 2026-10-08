namespace Marvel.Rules.Events;

/// <summary>A completed return to a player's deck, without hidden identities or positions.</summary>
/// <remarks>
/// The engine chooses this emitted-only receipt. PublicTitles records only faces public
/// before the move, sorted independently of deck order. Count includes unnamed cards.
/// </remarks>
public sealed record CardsShuffledIntoDeck(int Player, int Count, IReadOnlyList<string> PublicTitles) : GameEvent;
