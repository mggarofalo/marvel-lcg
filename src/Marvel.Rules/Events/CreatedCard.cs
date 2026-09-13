using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A card appearing, and enough to draw it.</summary>
/// <param name="Id">The object id.</param>
/// <param name="Card">The printed card id, e.g. <c>01001b</c>.</param>
/// <remarks>
/// The printed id travels with the event because a client receiving this over a
/// socket has never seen object 217 before and cannot look it up in a state it
/// does not yet have.
/// </remarks>
public readonly record struct CreatedCard(int Id, string Card);
