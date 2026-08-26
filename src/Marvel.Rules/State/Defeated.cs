namespace Marvel.Rules.State;

/// <summary>
/// How a card came to be defeated, while its "When Defeated" is resolving.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:defeat</c> says what a defeat <i>is</i> — "if a character has zero or
/// fewer remaining hit points, or if a side scheme has no threat on it" — and
/// nothing about what caused it, because nothing in the general rules needs to
/// know. Cards do. Gene Pool answers "after an ally is defeated <b>by anything
/// other than consequential damage</b>", and Targeted for Extermination hands a
/// status card to "<b>the player who defeated</b> this scheme".
/// </para>
/// <para>
/// So this is provenance and not state: it is set for the length of one defeat
/// and cleared afterwards, the same shape and for the same reason as
/// <see cref="EnemyAttack"/>. A card reading it outside that moment would be
/// reading the last defeat rather than none, which is why it goes back to null.
/// </para>
/// </remarks>
/// <param name="Card">The card that was defeated.</param>
/// <param name="By">
/// The seat whose character did it, or <c>-1</c> for the scenario.
/// <c>rr:ownership-and-control.2</c> — a card enters play under its owner's
/// control, so an ally's thwart is its owner's doing even though
/// <c>rr:you-your.15</c> keeps it off that player's identity.
/// </param>
/// <remarks>
/// <b>Who, and not yet how.</b> Gene Pool asks the other half — "after an ally
/// is defeated <i>by anything other than consequential damage</i>" — and it
/// cannot be authored until a defeat opens a response window for a forced
/// response to answer, so the field it would read is not here either.
/// MARVEL-248.
/// </remarks>
public sealed record Defeated(int Card, int By);
