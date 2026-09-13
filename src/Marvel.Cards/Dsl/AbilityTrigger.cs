using Marvel.Rules.Play;
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
