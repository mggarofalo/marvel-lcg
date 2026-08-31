namespace Marvel.Rules.State;

/// <summary>What kind of thing a printed face is.</summary>
/// <remarks>
/// One member per kind of printed face. The mapping from the card data's
/// <c>engine.type</c> preserves that printed identity even when two kinds obey
/// the same rules, as villains and leaders do.
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
    Status,
    EncounterVillain,
    EncounterSideScheme,
    Environment,
    Leader,
    Evidence,
    PlayerSideScheme,
    Challenge,
#pragma warning restore CS1591, SA1602
}

/// <summary>Rules-level relationships between printed card kinds.</summary>
public static class CardKinds
{
    /// <summary>Whether a printed kind functions as a villain.</summary>
    /// <remarks>
    /// <c>pack:mc56:leaders</c> and <c>pack:mc57:new-card-type-leader</c> call
    /// Leader a new card type, then say leaders function exactly like villains
    /// and every game rule and card ability affecting villains affects leaders.
    /// The kind stays distinct; this predicate is the shared rules meaning.
    /// </remarks>
    /// <param name="kind">The printed face kind.</param>
    public static bool IsVillain(CardKind kind) =>
        kind is CardKind.EncounterVillain or CardKind.Leader;

    /// <summary>Whether a printed kind is an enemy.</summary>
    /// <param name="kind">The printed face kind.</param>
    public static bool IsEnemy(CardKind kind) =>
        kind == CardKind.Minion || IsVillain(kind);

    /// <summary>Whether a printed kind is a character.</summary>
    /// <param name="kind">The printed face kind.</param>
    public static bool IsCharacter(CardKind kind) =>
        kind is CardKind.Hero or CardKind.AlterEgo or CardKind.Ally
        || IsEnemy(kind);
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
/// of a game — that is what makes it safe for the engine to consult.
/// </para>
/// </remarks>
public interface ICardFacts
{
    /// <summary>The linked cards brought by a deck containing this face.</summary>
    /// <remarks>
    /// <c>rr:linked-card-title</c> makes this printed product data: the linked
    /// card names the card that brings it, while setup starts from the bringing
    /// card. Most hand-built boards have none, so the default is empty.
    /// </remarks>
    IReadOnlyList<string> LinkedCards(string bringingFaceId) => [];

    /// <summary>What kind of card this face is.</summary>
    /// <param name="faceId">A printed card id.</param>
    CardKind Kind(string faceId);

    /// <summary>The printed encounter set this face belongs to, or an empty string.</summary>
    /// <remarks>
    /// This is printed acquisition data rather than a gameplay classification.
    /// A card can name the set of another card it just discarded, so the rules
    /// layer needs the fact without depending on the content dataset.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    string EncounterSet(string faceId) => string.Empty;

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

    /// <summary>
    /// The additional form this face grants its controller, or null.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:form-change-form.6</c>: cards with the "[type] form" keyword grant
    /// an identity unique forms. The keyword is printed, so this is printed
    /// data and belongs here — the name it returns is the <c>[type]</c>, lower
    /// cased: <c>energy</c>, <c>mass</c>, <c>suit</c>.
    /// </para>
    /// <para>
    /// <b>Defaulted, because nine faces out of 4,344 carry one.</b> A board
    /// assembled by hand that never sets up an additional form should not have
    /// to say so. The real catalog implements it, and
    /// <c>CardCatalogTests</c> pins the exact nine so that forgetting is a
    /// failing test rather than a silent absence.
    /// </para>
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    string? FormKeyword(string faceId) => null;

    /// <summary>
    /// How many consequential damage icons sit beneath one of an ally's
    /// powers — <c>rr:consequential-damage</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "After an ally attacks, it takes consequential damage equal to the
    /// number of consequential damage icons <b>beneath its ATK field</b>."
    /// Printed data, so it belongs here — the icons are stars printed in the
    /// same attribute as the value.
    /// </para>
    /// <para>
    /// Defaulted, like <see cref="FormKeyword"/>: only allies have them, and a
    /// board assembled by hand with no ally should not have to say so.
    /// </para>
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    /// <param name="attribute">The power, <c>ATK</c> or <c>THW</c>.</param>
    long ConsequentialDamage(string faceId, string attribute) => 0;

    /// <summary>
    /// The face's printed title.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Printed data, and the rules turn on it in places a name would look
    /// merely decorative. <c>rr:villain-defeat.3</c> and <c>.4</c> decide
    /// whether a defeated villain stage's attachments, status cards and tokens
    /// carry over by whether "the new stage of the villain has the <b>same
    /// title</b>"; <c>rr:identity.2</c> makes a card naming Angel not reach
    /// Archangel.
    /// </para>
    /// <para>
    /// Defaulted to the face id, so a board built by hand out of made-up faces
    /// gets titles that are distinct exactly when the faces are.
    /// </para>
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    string Title(string faceId) => faceId;

    /// <summary>
    /// The face's printed subtitle, or an empty string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The small line under the title — Wasp's ally card is "Wasp", subtitled
    /// "Janet van Dyne". <c>rr:team-up.2</c> is the rule that needs it: "an
    /// ally counts as a named character if <b>either its title or subtitle</b>
    /// matches the named character", so a hero's own ally card and the ally
    /// another player is running can be the same character under two names.
    /// </para>
    /// <para>
    /// Defaulted to empty, like <see cref="FormKeyword"/>: most cards have
    /// none, and a board built by hand should not have to say so.
    /// </para>
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    string Subtitle(string faceId) => string.Empty;

    /// <summary>
    /// Whether this face prints a "<b>Boost</b>" ability —
    /// <c>rr:boost-boost-icon.2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not derivable from the numbers.</b> The printed <c>Boost</c>
    /// attribute counts icons, and <c>rr:boost-boost-icon.1</c> makes a star
    /// icon "not a boost icon" that adds nothing — so a card with a boost
    /// ability and a card with none carry the same number, and the only place
    /// the star survives is the text box.
    /// </para>
    /// <para>
    /// Which is why this is its own question rather than a value: without it
    /// the engine cannot tell an unwritten boost ability from a card that has
    /// none, and 419 cards in the pool have one.
    /// </para>
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    bool HasBoostAbility(string faceId) => false;

    /// <summary>
    /// Whether this face prints a "<b>When Defeated</b>" ability —
    /// <c>rr:when-defeated-abilities</c>.
    /// </summary>
    /// <remarks>
    /// The same question <see cref="HasBoostAbility"/> asks and for the same
    /// reason: nothing in the printed attributes records it, so an unwritten
    /// ability and a card that has none look identical. 255 cards in the pool
    /// have one.
    /// </remarks>
    /// <param name="faceId">A printed card id.</param>
    bool HasWhenDefeated(string faceId) => false;
}
