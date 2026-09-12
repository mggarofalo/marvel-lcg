using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>The card is now a different face.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="From">The card id of the old face.</param>
/// <param name="To">The card id of the new face.</param>
/// <remarks>
/// A hero flipping to alter-ego, a villain changing stage. Distinct from
/// <see cref="CardsFlipped"/>, which is about which side is visible, not about
/// which card it is. A form change usually drags a batch of
/// <see cref="FieldSet"/> with it, because the two faces register different
/// stats — <c>attack</c> and <c>thwart</c> leave, <c>recover</c> arrives.
/// </remarks>
public sealed record CardFormChanged(int Card, string From, string To) : GameEvent;
