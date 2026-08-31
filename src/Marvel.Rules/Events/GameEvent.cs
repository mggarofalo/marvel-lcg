using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>
/// One thing that happened, in the engine's return value. <b>A wire type.</b>
/// </summary>
/// <remarks>
/// <para>
/// A board snapshot is enough to draw a board and not enough to animate one. It
/// can say the discard pile got taller; it cannot say that card 01096 went from
/// hand to discard because an ability's cost consumed it. That gap is what this
/// exists to close — see <c>docs/event-stream.md</c>.
/// </para>
/// <para>
/// <b>Derived, never maintained.</b> The interpreter emits these as a byproduct
/// of executing effect nodes. A parallel hand-written path drifts from the
/// rules, and then the animations start lying about what happened.
/// </para>
/// <para>
/// <b>No references into engine state.</b> Every payload is an integer, a
/// string or a list of them. Two reasons, and the second is the load-bearing
/// one: these cross a socket when the server is hosted rather than embedded, and
/// a record holding a live card reference lets the view layer walk the whole
/// state graph — including the hidden parts — through a field that was only
/// meant to say what moved.
/// </para>
/// <para>
/// The derivable subtype set is closed, and was chosen by measurement rather
/// than by taste: it is the smallest set that explained every state change
/// across a 201,870-transition sample of recorded play, with nothing left over
/// and no member that never fired. Emitted-only kinds are held separately:
/// they describe changes the engine knows happened and the digest cannot see.
/// <c>EventVocabularyTests</c> keeps both claims distinct.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CardsCreated), nameof(CardsCreated))]
[JsonDerivedType(typeof(CardsMoved), nameof(CardsMoved))]
[JsonDerivedType(typeof(AreaReordered), nameof(AreaReordered))]
[JsonDerivedType(typeof(CardFormChanged), nameof(CardFormChanged))]
[JsonDerivedType(typeof(CardsFlipped), nameof(CardsFlipped))]
[JsonDerivedType(typeof(CardAttached), nameof(CardAttached))]
[JsonDerivedType(typeof(CardDetached), nameof(CardDetached))]
[JsonDerivedType(typeof(ControlChanged), nameof(ControlChanged))]
[JsonDerivedType(typeof(FieldSet), nameof(FieldSet))]
[JsonDerivedType(typeof(PlayAreaJoined), nameof(PlayAreaJoined))]
public abstract record GameEvent
{
    /// <summary>
    /// The timing point that opened the decision this event came out of, e.g.
    /// <c>WhenPlayerInTurn</c>. Empty when nothing opened it.
    /// </summary>
    /// <remarks>
    /// Cause, not shape. A digest can be diffed to recover everything else on
    /// these records; it can never recover <i>why</i>, and why is what decides
    /// whether an animation is a card being played or a card being discarded to
    /// pay for one.
    /// </remarks>
    public string Trigger { get; init; } = "";

    /// <summary>
    /// The effect that ran, e.g. <c>Play</c>, <c>Attack</c>, <c>Change_Form</c>.
    /// Empty when the transition had no player-chosen effect behind it.
    /// </summary>
    public string Verb { get; init; } = "";
}

/// <summary>A card and where in its destination it landed.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="Index">Its position in the destination area, from 0.</param>
public readonly record struct Landing(int Card, int Index);

/// <summary>Cards entering the world.</summary>
/// <param name="Area">Where they appeared.</param>
/// <param name="Cards">The cards, ascending by object id.</param>
/// <remarks>
/// Object ids are never reused and <c>card_dict</c> is append-only, so there is
/// deliberately no counterpart event. A card removed from the game moves to the
/// removed area; it does not cease to exist. Measured: across 201,870 recorded
/// transitions, no card ever disappeared from the digest.
/// </remarks>
public sealed record CardsCreated(AreaRef Area, IReadOnlyList<CreatedCard> Cards)
    : GameEvent;

/// <summary>A card appearing, and enough to draw it.</summary>
/// <param name="Id">The object id.</param>
/// <param name="Card">The printed card id, e.g. <c>01001b</c>.</param>
/// <remarks>
/// The printed id travels with the event because a client receiving this over a
/// socket has never seen object 217 before and cannot look it up in a state it
/// does not yet have.
/// </remarks>
public readonly record struct CreatedCard(int Id, string Card);

/// <summary>A batch of cards crossing from one area to another.</summary>
/// <param name="From">The area they left.</param>
/// <param name="To">The area they entered.</param>
/// <param name="Cards">Each card and the slot it took, in destination order.</param>
/// <remarks>
/// One event per <c>(from, to)</c> pair, not one per card, because drawing five
/// cards is one thing that happened and should be one visual beat. It is also
/// the commonest move there is: <c>PlayerDeck -> HandsArea</c> was 24% of all
/// moves in the sample this was designed against.
/// </remarks>
public sealed record CardsMoved(AreaRef From, AreaRef To, IReadOnlyList<Landing> Cards)
    : GameEvent;

/// <summary>An area's order changed without anything entering or leaving it.</summary>
/// <param name="Area">The area.</param>
/// <param name="Order">Its complete new order, by object id.</param>
/// <remarks>
/// <para>
/// A shuffle, and one event for the area rather than one per card.
/// </para>
/// <para>
/// The distinction matters more than it looks. Taking a card out of the middle
/// of a deck shifts every card above it down by one, and a digest records that
/// as a position change for each of them — 20% of all observed change. Those are
/// <i>consequences</i> of the move, not separate things that happened, and an
/// animation that played them would be lying. Modelling the compaction instead
/// of emitting it removed 85% of apparent reorderings; what survives is a real
/// shuffle.
/// </para>
/// </remarks>
public sealed record AreaReordered(AreaRef Area, IReadOnlyList<int> Order) : GameEvent;

/// <summary>The card is now a different face.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="From">The card id of the old face.</param>
/// <param name="To">The card id of the new face.</param>
/// <remarks>
/// A hero flipping to alter-ego, a villain changing stage. Distinct from
/// <see cref="CardsFlipped"/>, which is about which side is visible, not about
/// which card it is. A form change usually drags a batch of
/// <see cref="FieldSet"/> with it, because the two faces register different
/// stats — <c>attack</c> and <c>thwart</c> leave, <c>recover</c> arrives.
/// </remarks>
public sealed record CardFormChanged(int Card, string From, string To) : GameEvent;

/// <summary>Cards turning face up or face down.</summary>
/// <param name="Cards">Object ids.</param>
/// <param name="FaceUp">Their new state.</param>
public sealed record CardsFlipped(IReadOnlyList<int> Cards, bool FaceUp) : GameEvent;

/// <summary>A card gained a host.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="Host">The card it is now attached to.</param>
public sealed record CardAttached(int Card, int Host) : GameEvent;

/// <summary>A card lost its host.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="Host">The card it was attached to.</param>
public sealed record CardDetached(int Card, int Host) : GameEvent;

/// <summary>A different player controls the card.</summary>
/// <param name="Card">The card's object id.</param>
/// <param name="From">The previous controller, or <c>-1</c>.</param>
/// <param name="To">The new controller, or <c>-1</c>.</param>
public sealed record ControlChanged(int Card, int From, int To) : GameEvent;

/// <summary>A play area joined a game area.</summary>
/// <param name="PlayArea">
/// The player's seat, or <c>-1</c> for the villain's play area.
/// </param>
/// <param name="GameArea">The destination game area's identity.</param>
/// <remarks>
/// <para>
/// One event for the play area, not one per card. A game area groups play
/// areas, so every card in the moving play area follows without moving between
/// card areas or changing any card field.
/// </para>
/// <para>
/// <b>Emitted-only.</b> A v2 digest cannot see game-area membership, so no
/// before/after digest comparison can derive this event. The engine can emit it
/// directly because <c>World.Join</c> performs the operation. The split between
/// derivable and emitted-only wire kinds is the engine's choice; the published
/// rule establishes the operation, not its JSON representation.
/// </para>
/// </remarks>
public sealed record PlayAreaJoined(int PlayArea, int GameArea) : GameEvent;


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
