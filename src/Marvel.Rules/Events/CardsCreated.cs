using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>Cards entering the world.</summary>
/// <param name="Area">Where they appeared.</param>
/// <param name="Cards">The cards, ascending by object id.</param>
/// <remarks>
/// Object ids are never reused and <c>card_dict</c> is append-only, so there is
/// deliberately no counterpart event. A card removed from the game moves to the
/// removed area; it does not cease to exist. Measured: across 201,870 recorded
/// transitions, no card ever disappeared from the digest.
/// </remarks>
public sealed record CardsCreated(AreaRef Area, IReadOnlyList<CreatedCard> Cards)
    : GameEvent;
