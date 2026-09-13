using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A different player controls the card.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="From">The previous controller, or <c>-1</c>.</param>
/// <param name="To">The new controller, or <c>-1</c>.</param>
public sealed record ControlChanged(int Card, int From, int To) : GameEvent;
