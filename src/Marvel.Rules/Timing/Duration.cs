namespace Marvel.Rules.Timing;

/// <summary>
/// How long an effect lasts, as the effect itself states it.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:lasting-effects</c>: "Some card abilities create effects or conditions
/// that affect the game for a specified duration <i>(such as 'until the end of
/// the phase' or 'until the end of this attack')</i>." The duration is part of
/// what the card says, so it is data on the effect rather than a category the
/// engine sorts effects into.
/// </para>
/// <para>
/// A duration is <b>not</b> only a timing point, which is the mistake this
/// replaces. <c>rr:delayed-effect.1</c> names both shapes in one sentence:
/// a delayed effect resolves after "their specified <i>timing point or future
/// condition</i> occurs or becomes true". And plenty of lasting effects are
/// bounded by use rather than by time — "the next card you play costs 1 less"
/// is spent when a card is played, whenever that is. So there are three bounds
/// and an effect may carry more than one:
/// </para>
/// <list type="table">
///   <item>
///     <term><see cref="Until"/></term>
///     <description>
///       a timing point — "until the end of the round".
///       <c>rr:lasting-effects.5</c>.
///     </description>
///   </item>
///   <item>
///     <term><see cref="OnCondition"/></term>
///     <description>
///       a future condition — "the next time an enemy attacks you".
///       <c>rr:delayed-effect.1</c>.
///     </description>
///   </item>
///   <item>
///     <term><see cref="Uses"/></term>
///     <description>
///       how many times it may still apply — "the next card", "the next two
///       times". Also the count on a condition: "the next 2 times X happens" is
///       <see cref="OnCondition"/> with <see cref="Uses"/> of 2.
///     </description>
///   </item>
/// </list>
/// <para>
/// <b>Whichever comes first.</b> "Reduce the cost of the next ally you play
/// this phase by 1" carries a timing point and a use count, and is gone at the
/// first of them. That is why these are three nullable bounds on one record and
/// not three kinds of effect.
/// </para>
/// </remarks>
/// <param name="Until">A timing point, or null if time does not bound it.</param>
/// <param name="OnCondition">A future condition, or null if no occurrence bounds it.</param>
/// <param name="Uses">How many applications remain, or null for unlimited.</param>
public sealed record Duration(string? Until = null, string? OnCondition = null, int? Uses = null)
{
    /// <summary>
    /// No stated duration: it lasts while its card is in play.
    /// </summary>
    /// <remarks>
    /// The constant ability case. <c>rr:ability</c>: "A constant ability becomes
    /// active as soon as its card enters play and remains active while the card
    /// is in play." A card leaving play is not a duration the card states — it
    /// is the general rule — so it is the absence of every bound here.
    /// </remarks>
    public static readonly Duration WhileInPlay = new();

    /// <summary>Whether nothing bounds this but its card staying in play.</summary>
    public bool IsWhileInPlay => Until is null && OnCondition is null && Uses is null;

    /// <summary>"Until the end of the round", and its like.</summary>
    /// <param name="timingPoint">One of <see cref="TimingPoints"/>.</param>
    public static Duration UntilEndOf(string timingPoint) => new(Until: timingPoint);

    /// <summary>"The next card you play", and its like.</summary>
    /// <param name="uses">How many times it may apply.</param>
    public static Duration NextUses(int uses) => new(Uses: uses);

    /// <summary>"The next time an enemy attacks you", and its like.</summary>
    /// <param name="condition">What must happen.</param>
    /// <param name="times">How many occurrences of it this survives.</param>
    public static Duration NextTime(string condition, int times = 1) =>
        new(OnCondition: condition, Uses: times);
}

/// <summary>
/// The moments a duration can name.
/// </summary>
/// <remarks>
/// The round's own structure, from <c>rr:round-overview</c>. Constants rather
/// than an enum because a duration is data that has to survive a save, and
/// because a scenario may name a point the base game does not have.
/// </remarks>
public static class TimingPoints
{
    /// <summary>
    /// The end of a player's turn.
    /// </summary>
    public const string EndOfTurn = "EndOfTurn";

    /// <summary>
    /// The end of the player phase — <c>rr:end-of-player-phase.step.4</c>.
    /// </summary>
    /// <remarks>
    /// <c>rr:player-phase.1</c> pins the moment exactly: these effects end
    /// <i>after</i> players draw up to their hand size and all cards are
    /// readied, not before.
    /// </remarks>
    public const string EndOfPlayerPhase = "EndOfPlayerPhase";

    /// <summary>The end of the villain phase — <c>rr:villain-phase.step.6.a</c>.</summary>
    public const string EndOfVillainPhase = "EndOfVillainPhase";

    /// <summary>
    /// The end of the round, which is the end of the villain phase —
    /// <c>rr:villain-phase.step.6</c> is titled "End of Villain Phase and
    /// Round", and both points are reached there.
    /// </summary>
    public const string EndOfRound = "EndOfRound";

    /// <summary>The end of one attack — the example <c>rr:lasting-effects</c> gives.</summary>
    public const string EndOfAttack = "EndOfAttack";

    /// <summary>
    /// The end of an enemy activation, of either kind —
    /// <c>rr:activation</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "There are two types of enemy activations: an attack activation and a
    /// scheme activation", and <c>rr:activation.6</c> gives them an ending
    /// outright — "that minion's activation <b>ends immediately</b> and no
    /// further steps of that activation resolve".
    /// </para>
    /// <para>
    /// Distinct from <see cref="EndOfAttack"/> because a scheme is an
    /// activation and is not an attack. A card that says "this activation" and
    /// was bounded by the end of an <i>attack</i> would survive a scheme
    /// activation entirely and go off during the next attack, against somebody
    /// it was never about.
    /// </para>
    /// </remarks>
    public const string EndOfActivation = "EndOfActivation";
}
