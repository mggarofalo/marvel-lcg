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
/// <para>
/// <b>Null for a constant ability, and only for one.</b> <c>rr:ability.5</c>
/// splits abilities in two by exactly this: one "prefaced by a bold timing
/// trigger followed by a colon" is triggered, and "an ability without a bold
/// timing trigger is referred to as a constant ability". A constant is not
/// timed to an occurrence at all — it "becomes active as soon as its card
/// enters play and remains active while the card is in play" — so there is no
/// condition to name, and naming one anyway would be a triggering condition
/// nothing produces sitting in the data looking implemented.
/// </para>
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
/// <param name="Form">
/// The form the player must be in, or null. <c>rr:player-turn.5.1</c>: "if the
/// action ability is preceded by <b>Hero</b> or <b>Alter-Ego</b>, the player
/// must be in the specified form in order to trigger the ability" — and 728 of
/// the 966 action abilities in the pool are preceded by one.
/// </param>
/// <param name="Also">
/// A second triggering condition the same occurrence must carry, or null.
/// <para>
/// <c>rr:triggering-condition.2</c> — "if a single game occurrence creates
/// multiple triggering conditions <i>(such as a single attack causing a
/// character to both take damage and be defeated)</i>, those triggering
/// conditions are handled with a single interrupt window and a single response
/// window." Prelate Sidearm's "after Unus <b>attacks and</b> defeats an ally"
/// is a sentence about that pair: the subject says <i>which</i> enemy and this
/// says the enemy was attacking rather than being attacked, which
/// <c>rr:retaliate</c> is the case that tells apart.
/// </para>
/// <para>
/// The same vocabulary <see cref="Event"/> uses, and held against the same set.
/// </para>
/// </param>
/// <param name="Player">
/// Whose opportunity the ability is, or null for its card's controller.
/// <para>
/// The one value is <c>trigger.player</c> — the seat the occurrence happened
/// to. <c>rr:ability.8</c> lets <i>any</i> player trigger an optional ability
/// on an encounter card, so the scenario owning a card is not by itself a
/// reason to narrow it; what narrows it is the card saying "you", which
/// <c>rr:you-your.7</c> points at the player the occurrence happened to rather
/// than at an owner the card has not got. Both are things an encounter card can
/// say, so the card says which.
/// </para>
/// </param>
public sealed record AbilityTrigger(
    string? Event,
    AbilityType Timing,
    string Subject,
    string? Form = null,
    string? Also = null,
    string? Player = null);

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

    /// <summary>
    /// The occurrence is not about a card at all.
    /// </summary>
    /// <remarks>
    /// <c>rr:triggering-condition</c> makes a condition "a specific occurrence
    /// that takes place in the game", and some of those happen to nobody:
    /// Hunting Gene Traitors answers "after resolving step one of the villain
    /// phase", which names a moment and nothing in it. There is nothing to
    /// match, so a card that answers such a moment says <b>so</b> rather than
    /// being handed a subject it does not have — <c>you</c> would fit by
    /// accident here, because an encounter card's owner and an unattributed
    /// occurrence's player are both the scenario.
    /// </remarks>
    public const string Game = "game";

    /// <summary>Every subject this vocabulary has.</summary>
    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { This, AttachedTo, You, Game };
}

/// <summary>The players an ability can name.</summary>
/// <remarks>
/// Shared between the trigger's <c>player</c> and the effect tree's, so the
/// same phrase means the same seat wherever a card writes it.
/// </remarks>
public static class AbilityPlayers
{
    /// <summary>The seat the occurrence happened to.</summary>
    public const string TriggerPlayer = "trigger.player";

    /// <summary>The seat resolving the ability.</summary>
    public const string You = "you";

    /// <summary>The seat that controls the ability's card.</summary>
    public const string Controller = "controller";
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
/// <param name="Cost">
/// What must be paid to use it, or null. <c>rr:cost</c> — "a cost is anything a
/// player must do or pay in order to initiate an ability" — and
/// <c>rr:initiating-abilities.step.5</c> makes paying it a step of its own,
/// aborted "without paying any costs" if it cannot be met. 560 of the 966 action
/// abilities in the pool print one, and by far the commonest is exhausting the
/// card the ability is on.
/// </param>
/// <param name="Limit">
/// How many times per round this ability may be used, or null for no limit.
/// <c>rr:limit</c> — "each copy of an ability with such a limit may be used X
/// times per the specified period, <b>per instance of that ability</b>", so the
/// count is per card in play rather than per printed id.
/// </param>
public sealed record CardAbility(
    string Card, string Name, AbilityTrigger Trigger, AbilityNode Effect,
    AbilityNode? Cost = null, long? Limit = null);

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
/// <param name="AttachTo">
/// What each card that prints "attach to" names, by printed face id.
/// <para>
/// <b>Not an ability, and that is the point.</b> <c>rr:attach-to</c> is a rule
/// about the phrase — "if a card uses the phrase 'attach to', it must be
/// attached to the specified game element <b>as it enters play</b>" — so the
/// engine does the attaching on every path into play and the card supplies only
/// the element. Modelling it as a "When Revealed" instead reads correctly for a
/// card revealed off the encounter deck and is wrong everywhere else:
/// <c>rr:when-revealed-abilities.2</c> says a card put into play without being
/// revealed does not trigger one, and a setup attachment is put into play
/// without being revealed.
/// </para>
/// </param>
public sealed record AbilityBook(
    IReadOnlyList<CardAbility> Abilities,
    IReadOnlySet<string> Authored,
    IReadOnlyDictionary<string, AbilityValue>? AttachTo = null)
{
    /// <summary>An empty book. No card has been read.</summary>
    public static AbilityBook None { get; } =
        new([], new HashSet<string>(StringComparer.Ordinal));

    /// <summary>What one card's "attach to" names, or null when it prints none.</summary>
    /// <param name="card">A printed face id.</param>
    public AbilityValue? Attaches(string card) =>
        AttachTo is { } named && named.TryGetValue(card, out var element) ? element : null;

    /// <summary>The abilities on one printed face.</summary>
    /// <param name="card">A printed face id.</param>
    public IEnumerable<CardAbility> On(string card) =>
        Abilities.Where(ability => string.Equals(ability.Card, card, StringComparison.Ordinal));
}
