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
[JsonDerivedType(typeof(PlayAreaDetached), nameof(PlayAreaDetached))]
public abstract record GameEvent
{
    /// <summary>Occurrence-time public names for card subjects, keyed by object id.</summary>
    /// <remarks>
    /// This optional evidence preserves an effective identity across later transitions in
    /// the same response. The view still authorizes the event before this data crosses the
    /// wire; an absent entry deliberately falls back to the resulting snapshot.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<int, string>? Subjects { get; init; }

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
