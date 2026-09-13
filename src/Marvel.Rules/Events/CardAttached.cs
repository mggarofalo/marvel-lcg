using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A card gained a host.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="Host">The card it is now attached to.</param>
public sealed record CardAttached(int Card, int Host) : GameEvent;
