using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>
/// When an ability fires: what happened, in which tier, and to whom.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/card-dsl.md</c>'s Layer 0 — the envelope, which "is already data;
/// this only writes down its shape". The 303 <c>AbilityFactory</c> methods in
/// the Python engine factor into these three fields.
/// </para>
/// <para>
/// <b><see cref="Event"/> is a triggering condition, spelled as the engine
/// spells it.</b> <c>rr:triggering-condition</c> calls one "a specific
/// occurrence that takes place in the game", and that is a rules vocabulary
/// rather than an implementation detail — so a card names <c>WhenEnemyAttacks</c>
/// rather than a DSL word that has to be translated into it. A translation table
/// is a second vocabulary, and a second vocabulary drifts.
/// </para>
/// </remarks>
/// <param name="Event">
/// The triggering condition, e.g. <c>WhenEnemyAttacks</c>. Held against the
/// conditions the engine's steps actually produce, so an event nothing fires is
/// a failing test rather than a card that never triggers.
/// </param>
/// <param name="Timing">
/// The bold trigger the card prints — "Forced Interrupt", "When Revealed".
/// <c>docs/card-dsl.md</c> says the timing must be the engine's existing
/// enumeration and "must not become" a new invention; this is that enumeration,
/// one level down. A card prints its <i>type</i> and the tier is derived from it
/// by <see cref="AbilityTypes.PriorityOf"/>, which is the direction the rules
/// read: <c>rr:ability</c> lists types and gives them an order.
/// </param>
/// <param name="Subject">One of <see cref="AbilitySubjects"/>.</param>
public sealed record AbilityTrigger(string Event, AbilityType Timing, string Subject);

/// <summary>
/// Which occurrences an ability answers, out of all those with its condition.
/// </summary>
/// <remarks>
/// A closed set, because this is the part of a card most likely to grow an
/// escape hatch. "When <b>Rhino</b> attacks" and "when the villain attacks
/// <b>you</b>" are two different relations between a card and an occurrence, and
/// naming them is what stops the DSL acquiring a general predicate.
/// </remarks>
public static class AbilitySubjects
{
    /// <summary>The occurrence is about this card. A card's own "When Revealed".</summary>
    public const string This = "this";

    /// <summary>
    /// The occurrence is about the card this one is attached to. Charge's
    /// "When <b>Rhino</b> attacks", where Rhino is whatever it is attached to —
    /// <c>rr:star-icon.2</c> reads the star as being about "the attached enemy".
    /// </summary>
    public const string AttachedTo = "attachedTo";

    /// <summary>
    /// The occurrence happened to this card's controller. Spider-Sense's "an
    /// attack against <b>you</b>", which <c>rr:attack-enemy-activation.1.4</c>
    /// makes a claim about the attacked <i>player</i> whichever character was
    /// targeted.
    /// </summary>
    public const string You = "you";

    /// <summary>Every subject this vocabulary has.</summary>
    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { This, AttachedTo, You };
}

/// <summary>
/// One printed ability, as data.
/// </summary>
/// <param name="Card">The printed face id it is on, e.g. <c>01099</c>.</param>
/// <param name="Name">
/// The card's own name for it, which is what a player chooses between —
/// <c>rr:labeled-ability</c>, the label before the dash. Spider-Man's is
/// "Spider-Sense". Falls back to the card's name where the ability has none.
/// </param>
/// <param name="Trigger">When it fires.</param>
/// <param name="Effect">What it does.</param>
/// <remarks>
/// <c>docs/card-dsl.md</c>'s envelope has four more fields — <c>when</c>,
/// <c>cost</c>, <c>target</c> and <c>limit</c>. None is here, because none of
/// the cards authored so far carries one, and a field with no card behind it is
/// a guess at a shape. They are the next things to add and each is additive:
/// the deserialiser refuses an ability carrying a field it does not know, so a
/// card that needs one fails rather than silently losing it.
/// </remarks>
public sealed record CardAbility(
    string Card, string Name, AbilityTrigger Trigger, AbilityNode Effect);

/// <summary>
/// Every authored card, and every ability on them.
/// </summary>
/// <remarks>
/// <b><see cref="Authored"/> is not the same as "has an ability".</b> Most
/// encounter cards have no "When Revealed" at all, and a card that has been
/// read and found to have none is a different thing from one nobody has looked
/// at. Without the distinction an unported card silently does nothing, which is
/// the failure this whole codebase throws rather than allow.
/// </remarks>
/// <param name="Abilities">Every ability, in the order the data lists them.</param>
/// <param name="Authored">Every card that has been read, whether or not it does anything.</param>
public sealed record AbilityBook(
    IReadOnlyList<CardAbility> Abilities, IReadOnlySet<string> Authored)
{
    /// <summary>An empty book. No card has been read.</summary>
    public static AbilityBook None { get; } =
        new([], new HashSet<string>(StringComparer.Ordinal));

    /// <summary>The abilities on one printed face.</summary>
    /// <param name="card">A printed face id.</param>
    public IEnumerable<CardAbility> On(string card) =>
        Abilities.Where(ability => string.Equals(ability.Card, card, StringComparison.Ordinal));
}
