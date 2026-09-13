namespace Marvel.Rules.Timing;

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
