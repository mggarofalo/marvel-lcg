namespace Marvel.Rules.State;

/// <summary>What kind of thing a printed face is.</summary>
/// <remarks>
/// One member per face class the Python engine instantiates. The mapping from
/// the card data's <c>engine.type</c> is the identity on all but two:
/// <c>Villain</c> and <c>SideScheme</c> each have a player and an encounter
/// variant, and only the encounter one appears in a scenario's own cards.
/// </remarks>
public enum CardKind
{
#pragma warning disable CS1591, SA1602
    Unknown = 0,
    Insert,
    AlterEgo,
    Hero,
    Ally,
    Event,
    Resource,
    Support,
    Upgrade,
    Attachment,
    Obligation,
    Treachery,
    Minion,
    MainScheme,
    EncounterVillain,
    EncounterSideScheme,
#pragma warning restore CS1591, SA1602
}

/// <summary>
/// What the rules need to know about a printed card.
/// </summary>
/// <remarks>
/// <para>
/// An interface rather than a type, so that <c>Marvel.Rules</c> does not
/// reference the content assembly. The layering in
/// <c>docs/presentation-layer.md</c> puts content <b>above</b> the rules, so the
/// arrow has to point this way: the rules say what they need, and the content
/// assembly satisfies it.
/// </para>
/// <para>
/// Everything here is printed. Nothing on this interface may depend on the state
/// of a game — that is what makes it safe for the fold to consult.
/// </para>
/// </remarks>
public interface ICardFacts
{
    /// <summary>What kind of card this face is.</summary>
    /// <param name="faceId">A printed card id.</param>
    CardKind Kind(string faceId);

    /// <summary>The printed traits, upper-cased as the digest spells them.</summary>
    /// <param name="faceId">A printed card id.</param>
    IReadOnlyList<string> Traits(string faceId);

    /// <summary>
    /// The printed attribute table — <c>HP</c>, <c>ATK</c>, <c>Stage</c> and so on.
    /// </summary>
    /// <remarks>
    /// Values are the raw printed strings, including the per-player <c>*</c>
    /// suffix. <see cref="PrintedValue"/> resolves them.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    IReadOnlyDictionary<string, string> Attributes(string faceId);

    /// <summary>
    /// One printed attribute as a number, or <paramref name="fallback"/> when it
    /// is absent or not numeric.
    /// </summary>
    /// <remarks>
    /// <c>*</c> means "per player": the engine substitutes the player count for
    /// each <c>*</c> and evaluates, so <c>14*</c> at three players is 42 and
    /// <c>1**</c> is 9. Reproduced rather than reinterpreted — it decides
    /// villain hit points, and villain hit points decide games.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    /// <param name="attribute">The attribute name, e.g. <c>HP</c>.</param>
    /// <param name="players">How many players are in the game.</param>
    /// <param name="fallback">What to answer when there is no such number.</param>
    long PrintedValue(string faceId, string attribute, int players, long fallback = 0);
}
