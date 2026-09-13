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
