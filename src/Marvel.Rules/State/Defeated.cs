namespace Marvel.Rules.State;

/// <summary>
/// How a card came to be defeated — <c>rr:defeat</c>.
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
/// <b>It lives on the occurrence, not on the board.</b> A response to a defeat
/// is offered after the defeat and after everything else that occurrence did,
/// so provenance has to outlast the call that made it — and a field on
/// <see cref="World"/> would have to be set before and cleared after, which is
/// the half nobody remembers. <c>Timing.Occurrence.Defeats</c> lasts exactly as
/// long as the two windows do, which is exactly as long as anything can ask.
/// </para>
/// </remarks>
/// <param name="Card">The card that was defeated.</param>
/// <param name="By">
/// The seat whose identity performed the defeating function, or <c>-1</c>
/// when no identity did. <c>rr:you-your.15</c>: functions performed by allies
/// are not considered to be performed by the controlling player's identity.
/// </param>
/// <param name="How">
/// What kind of thing did it, in the verb the event stream records: an attack,
/// consequential damage, indirect damage, a card dealing damage outright, or a
/// thwart that took a side scheme's last threat.
/// <para>
/// One string rather than an enum because it <b>is</b> the event stream's verb
/// and not a second spelling of it. A card asking about it — Gene Pool's "by
/// anything other than consequential damage" — names the rule it means, and the
/// interpreter maps that name to the verb; so the vocabulary a card may use is
/// closed and checked without this having to be.
/// </para>
/// </param>
public sealed record Defeated(int Card, int By, string How)
{
    /// <summary>The player responsible for the defeating action, including controlled allies and supports.</summary>
    /// <remarks>This immutable occurrence fact defaults to identity credit at construction.
    /// It is independent of By, which remains the predicate for identity-only responses.
    /// Occurrences are reconstructed by replay and are not persisted session authority.</remarks>
    public int ResponsiblePlayer { get; init; } = By;
    /// <summary>The classification at defeat, before leaving play clears temporary profiles.</summary>
    public required CardKind Kind { get; init; }
    /// <summary>The public name at defeat, including temporary character names.</summary>
    public string? Title { get; init; }
    /// <summary>The copy that was defeated, for response target provenance.</summary>
    public int Incarnation { get; init; } = -1;
    /// <summary>The controller before departure returns the card to its owner.</summary>
    public int Controller { get; init; } = -1;
    /// <summary>The temporary assignment that ended in this defeat.</summary>
    public string? ProfileId { get; init; }
    /// <summary>The physical facing at the triggering condition.</summary>
    public bool WasFaceDown { get; init; }
}
