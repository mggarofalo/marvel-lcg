using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;


/// <summary>One named value on a card changed.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="Field">The field name, e.g. <c>health</c> or <c>t_AVENGER</c>.</param>
/// <param name="From">Its previous value, or <c>null</c> if it did not exist.</param>
/// <param name="To">Its new value, or <c>null</c> if it no longer exists.</param>
/// <remarks>
/// The open-ended one, and the busiest: 22% of observed change is a field
/// changing value and another 15% is one appearing or disappearing. Absent and
/// zero are different — a field that is gone means the card no longer registers
/// it at all, which is how a trait grant expires.
/// </remarks>
public sealed record FieldSet(int Card, string Field, long? From, long? To) : GameEvent;
