namespace Marvel.Rules.Timing;

/// <summary>One ability waiting in a window.</summary>
/// <param name="Card">The object id of the card carrying it.</param>
/// <param name="Type">Its bold timing trigger.</param>
/// <param name="Player">
/// The seat that would resolve it, or <c>-1</c> for an ability on an encounter
/// card that no player has claimed. <c>rr:ability.8</c> lets any player use an
/// optional ability on an encounter card, so who resolves it is settled when it
/// is offered, not when it is collected.
/// </param>
/// <param name="Ordinal">
/// Which ability at this timing on the card is waiting, in printed/data order.
/// Most cards have one and therefore use zero.
/// </param>
public readonly record struct PendingAbility(
    int Card, AbilityType Type, int Player, int Ordinal = 0);
