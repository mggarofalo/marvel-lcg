using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>An area's order changed without anything entering or leaving it.</summary>
/// <param name="Area">The area.</param>
/// <param name="Order">Its complete new order, by object id.</param>
/// <remarks>
/// <para>
/// A shuffle, and one event for the area rather than one per card.
/// </para>
/// <para>
/// The distinction matters more than it looks. Taking a card out of the middle
/// of a deck shifts every card above it down by one, and a digest records that
/// as a position change for each of them — 20% of all observed change. Those are
/// <i>consequences</i> of the move, not separate things that happened, and an
/// animation that played them would be lying. Modelling the compaction instead
/// of emitting it removed 85% of apparent reorderings; what survives is a real
/// shuffle.
/// </para>
/// </remarks>
public sealed record AreaReordered(AreaRef Area, IReadOnlyList<int> Order) : GameEvent;
