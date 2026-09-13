namespace Marvel.Rules.Timing;

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

    /// <summary>The end of whichever player or villain phase is current.</summary>
    public const string EndOfPhase = "EndOfPhase";

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
