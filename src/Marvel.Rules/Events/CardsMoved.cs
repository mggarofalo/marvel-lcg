using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A batch of cards crossing from one area to another.</summary>
/// <param name="From">The area they left.</param>
/// <param name="To">The area they entered.</param>
/// <param name="Cards">Each card and the slot it took, in destination order.</param>
/// <remarks>
/// One event per <c>(from, to)</c> pair, not one per card, because drawing five
/// cards is one thing that happened and should be one visual beat. It is also
/// the commonest move there is: <c>PlayerDeck -> HandsArea</c> was 24% of all
/// moves in the sample this was designed against.
/// </remarks>
public sealed record CardsMoved(AreaRef From, AreaRef To, IReadOnlyList<Landing> Cards)
    : GameEvent;
