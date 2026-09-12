using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A card lost its host.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="Host">The card it was attached to.</param>
public sealed record CardDetached(int Card, int Host) : GameEvent;
