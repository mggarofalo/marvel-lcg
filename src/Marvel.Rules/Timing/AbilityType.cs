namespace Marvel.Rules.Timing;

/// <summary>
/// The bold timing trigger an ability is prefaced by, or its absence.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:ability.5</c>: an ability prefaced by a bold timing trigger followed
/// by a colon is a <i>triggered</i> ability; one without is a <i>constant</i>
/// ability. This enumerates the triggers the Rules Reference names.
/// </para>
/// <para>
/// The type decides two things and nothing else: when the ability acts
/// (<see cref="AbilityTypes.PriorityOf"/>) and whether anyone gets a choice
/// about it (<see cref="AbilityTypes.IsMandatory"/>). Everything else about an
/// ability is the card's business.
/// </para>
/// </remarks>
public enum AbilityType
{
    /// <summary>No bold trigger. Active while its card is in play — <c>rr:ability.5</c>.</summary>
    Constant,

    /// <summary>A keyword. Mandatory, and resolves with the constant abilities — <c>rr:ability.7</c>.</summary>
    Keyword,

    /// <summary>"Setup", resolved during setup — <c>rr:setup-triggered-ability</c>.</summary>
    Setup,

    /// <summary>"Resource", triggerable while paying a cost — <c>rr:resource-ability.1</c>.</summary>
    Resource,

    /// <summary>"Action" — <c>rr:action</c>.</summary>
    Action,

    /// <summary>"Forced Action", which the player phase cannot end with outstanding — <c>rr:action.2</c>.</summary>
    ForcedAction,

    /// <summary>"Interrupt" — <c>rr:interrupt</c>.</summary>
    Interrupt,

    /// <summary>"Forced Interrupt" — <c>rr:forced.1</c>.</summary>
    ForcedInterrupt,

    /// <summary>
    /// "Forced Interrupt" printed on a status card, which goes ahead of every
    /// other forced interrupt — <c>rr:ability.step.2.a</c>.
    /// </summary>
    StatusForcedInterrupt,

    /// <summary>
    /// "When Defeated". <c>rr:when-defeated-abilities.1</c> defines it as
    /// exactly "Forced Interrupt: When this card is defeated…", which is why
    /// the card leaves play <i>after</i> it resolves
    /// (<c>rr:when-defeated-abilities.2.1</c>).
    /// </summary>
    WhenDefeated,

    /// <summary>
    /// "When Completed". <c>rr:when-completed-abilities.1</c> defines it as
    /// "Forced Interrupt: When this scheme is completed…".
    /// </summary>
    WhenCompleted,

    /// <summary>"Boost" — <c>rr:boost-boost-icon</c>.</summary>
    Boost,

    /// <summary>"When Revealed" — <c>rr:when-revealed-abilities</c>.</summary>
    WhenRevealed,

    /// <summary>"Response" — <c>rr:response</c>.</summary>
    Response,

    /// <summary>"Forced Response" — <c>rr:forced.1</c>.</summary>
    ForcedResponse,

    /// <summary>"Special", whose timing the card itself states — <c>rr:special</c>.</summary>
    Special,
}

/// <summary>When each <see cref="AbilityType"/> acts, and whether it is a choice.</summary>
public static class AbilityTypes
{
    /// <summary>
    /// The tier this type resolves in, around the occurrence it is timed to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Straight off the list at the head of <c>rr:ability</c>. Two of these are
    /// worth stating out loud, because they are easy to place a tier late and
    /// that is the kind of wrong that produces a plausible board:
    /// </para>
    /// <para>
    /// <b>"When Defeated" and "When Completed" are forced interrupts.</b>
    /// <c>rr:when-defeated-abilities.1</c> and
    /// <c>rr:when-completed-abilities.1</c> define both as exactly that, so they
    /// resolve <i>before</i> the defeat or the completion, alongside every other
    /// forced interrupt. Grouped with Boost and When Revealed instead — a tier
    /// too late — a villain's dying ability would resolve after it had already
    /// left play.
    /// </para>
    /// <para>
    /// <b>A status card's forced interrupt is its own tier</b>
    /// (<c>rr:ability.step.2.a</c>), ahead of ordinary forced interrupts. That
    /// is what makes Stun, Confuse and Tough beat whatever else wants the same
    /// window.
    /// </para>
    /// </remarks>
    /// <param name="type">The bold trigger.</param>
    public static TimingPriority PriorityOf(AbilityType type) => type switch
    {
        AbilityType.Constant or AbilityType.Keyword => TimingPriority.Continuous,

        AbilityType.StatusForcedInterrupt => TimingPriority.StatusForcedInterrupt,

        AbilityType.ForcedInterrupt or AbilityType.WhenDefeated or AbilityType.WhenCompleted
            => TimingPriority.ForcedInterrupt,

        AbilityType.Interrupt => TimingPriority.Interrupt,

        AbilityType.Boost or AbilityType.WhenRevealed => TimingPriority.Occurrence,

        AbilityType.ForcedResponse => TimingPriority.ForcedResponse,
        AbilityType.Response => TimingPriority.Response,

        // Not timed around an occurrence at all. An action is taken during a
        // player's turn, a resource ability while a cost is being paid, a setup
        // ability during setup, and a "Special" whenever its own card says. They
        // are ability types with no place on this list, and answering with a
        // tier anyway would put them in windows they do not belong in.
        _ => TimingPriority.Untimed,
    };

    /// <summary>Whether the game resolves this without asking anyone.</summary>
    /// <remarks>
    /// <c>rr:ability.7</c> lists the mandatory types and <c>rr:ability.8</c> the
    /// optional ones; <c>rr:ability.11</c> states the rule the two lists follow,
    /// that everything is optional unless prefaced by "Forced".
    /// <c>rr:ability.7.1</c> does not change this classification: a mandatory
    /// ability using the word “may” remains mandatory to initiate, while the
    /// part after “may” is represented as an optional choice inside its effect
    /// tree because that choice is a property of the printed text, not its type.
    /// </remarks>
    /// <param name="type">The bold trigger.</param>
    public static bool IsMandatory(AbilityType type) => type is
        AbilityType.Constant or AbilityType.Keyword or AbilityType.Setup or
        AbilityType.WhenRevealed or AbilityType.WhenDefeated or AbilityType.WhenCompleted or
        AbilityType.ForcedAction or AbilityType.ForcedInterrupt or
        AbilityType.StatusForcedInterrupt or AbilityType.ForcedResponse or
        AbilityType.Boost;

    /// <summary>Whether this type acts in an interrupt window.</summary>
    /// <param name="type">The bold trigger.</param>
    public static bool IsInterrupt(AbilityType type) =>
        PriorityOf(type) is TimingPriority.StatusForcedInterrupt
            or TimingPriority.ForcedInterrupt or TimingPriority.Interrupt;

    /// <summary>Whether this type acts in a response window.</summary>
    /// <param name="type">The bold trigger.</param>
    public static bool IsResponse(AbilityType type) =>
        PriorityOf(type) is TimingPriority.ForcedResponse or TimingPriority.Response;
}
