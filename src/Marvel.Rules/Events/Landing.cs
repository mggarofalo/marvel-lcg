using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A card and where in its destination it landed.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="Index">Its position in the destination area, from 0.</param>
public readonly record struct Landing(int Card, int Index);
