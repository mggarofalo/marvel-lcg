using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>
/// When an ability fires: what happened, in which tier, and to whom.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/card-dsl.md</c>'s Layer 0 — the envelope, which "is already data;
/// this only writes down its shape". Every way a card can be triggered factors
/// into these three fields: what happened, in which tier, and to whom.
/// </para>
/// <para>
/// <b><see cref="Event"/> is a triggering condition, spelled as the engine
/// spells it.</b> <c>rr:triggering-condition</c> calls one "a specific
/// occurrence that takes place in the game", and that is a rules vocabulary
/// rather than an implementation detail — so a card names <c>WhenAttackInitiated</c>
/// rather than a DSL word that has to be translated into it. A translation table
/// is a second vocabulary, and a second vocabulary drifts.
/// </para>
/// </remarks>
/// <param name="Event">
/// The triggering condition, e.g. <c>WhenAttackInitiated</c>. Held against the
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
/// <param name="Subject">One of <see cref="AbilitySubjects"/>, or null.</param>
/// <param name="Actor">Which attacking card may fill the actor role, or null.</param>
/// <param name="Target">Which attacked card may fill the target role, or null.</param>
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
    string? Subject,
    string? Actor = null,
    string? Target = null,
    string? Form = null,
    string? Also = null,
    string? Player = null);

/// <summary>Which card may fill an occurrence's actor or target role.</summary>
public static class AbilityRoles
{
    /// <summary>The card carrying the ability.</summary>
    public const string This = "this";

    /// <summary>The card this ability's card is attached to.</summary>
    public const string AttachedTo = "attachedTo";

    /// <summary>
    /// A player-controlled card. A player card also requires the same
    /// controller; an encounter card binds its resolver to this role's
    /// controller.
    /// </summary>
    public const string You = "you";

    /// <summary>A villain.</summary>
    public const string Villain = "villain";

    /// <summary>A minion.</summary>
    public const string Minion = "minion";

    /// <summary>A hero.</summary>
    public const string Hero = "hero";

    /// <summary>An ally.</summary>
    public const string Ally = "ally";

    /// <summary>Any card controlled by a player — <c>rr:friendly</c>.</summary>
    public const string Friendly = "friendly";

    /// <summary>A villain or minion.</summary>
    public const string Enemy = "enemy";

    /// <summary>Every role matcher this vocabulary has.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        This, AttachedTo, You, Villain, Minion, Hero, Ally, Friendly, Enemy,
    };
}

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
/// The implemented envelope carries the trigger, an optional live condition,
/// a cost and a per-round limit. Target selection remains an effect-tree
/// question until a printed card requires a target in the envelope itself.
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
/// <param name="When">An additional printed condition that must currently be true.</param>
/// <param name="AnyPlayer">
/// Whether the printed ability explicitly permits any player to initiate it.
/// This is card text overriding the ordinary controller or attachment-holder
/// permission, not a conclusion inferred from the card's ownership.
/// </param>
/// <param name="Labels">
/// Every parenthetical attack, defense, or thwart label printed on the
/// ability. Multiple labels belong to one ability and are resolved together.
/// </param>
/// <param name="PrintedResources">
/// Resource icons physically printed in this resource ability's text box.
/// Empty when its generated resources are not printed icons there.
/// </param>
/// <param name="Maximum">
/// A use maximum shared across every copy of this card by title, or null.
/// </param>
public sealed record CardAbility(
    string Card, string Name, AbilityTrigger Trigger, AbilityNode Effect,
    AbilityNode? Cost = null, long? Limit = null, AbilityNode? When = null,
    bool AnyPlayer = false, IReadOnlyList<string>? Labels = null,
    string PrintedResources = "", AbilityMaximum? Maximum = null);

/// <summary>A maximum shared across copies of a card by title.</summary>
public sealed record AbilityMaximum(long Uses, MaximumPeriod Period);

/// <summary>The period printed by a card maximum.</summary>
public enum MaximumPeriod
{
    /// <summary>Until the end of the round.</summary>
    Round,
    /// <summary>Until the end of the current phase.</summary>
    Phase,
    /// <summary>For the rest of the game.</summary>
    Game,
    /// <summary>For one triggering occurrence.</summary>
    Instance,
}

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
/// <param name="ControlledByFirstPlayer">
/// Cards whose setup text gives control to the first player. This is placement
/// metadata, not a claim that the rest of the card's abilities are authored.
/// </param>
/// <param name="PlacementOnly">
/// Cards whose placement text and absence of a When Revealed ability are known,
/// while their other printed abilities remain unauthored.
/// </param>
public sealed record AbilityBook(
    IReadOnlyList<CardAbility> Abilities,
    IReadOnlySet<string> Authored,
    IReadOnlyDictionary<string, AbilityValue>? AttachTo = null,
    IReadOnlySet<string>? ControlledByFirstPlayer = null,
    IReadOnlySet<string>? PlacementOnly = null)
{
    /// <summary>An empty book. No card has been read.</summary>
    public static AbilityBook None { get; } =
        new([], new HashSet<string>(StringComparer.Ordinal));

    /// <summary>What one card's "attach to" names, or null when it prints none.</summary>
    /// <param name="card">A printed face id.</param>
    public AbilityValue? Attaches(string card) =>
        AttachTo is { } named && named.TryGetValue(card, out var element) ? element : null;

    /// <summary>Whether setup gives this card to the first player.</summary>
    public bool FirstPlayerControls(string card) =>
        ControlledByFirstPlayer?.Contains(card) is true;

    /// <summary>Whether enough is known to resolve this card's reveal as silence.</summary>
    public bool KnowsWhenRevealed(string card) =>
        Authored.Contains(card) || PlacementOnly?.Contains(card) is true;

    /// <summary>The abilities on one printed face.</summary>
    /// <param name="card">A printed face id.</param>
    public IEnumerable<CardAbility> On(string card) =>
        Abilities.Where(ability => string.Equals(ability.Card, card, StringComparison.Ordinal));
}
