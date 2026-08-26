namespace Marvel.Rules.Timing;

/// <summary>
/// The order in which abilities act around one game occurrence.
/// </summary>
/// <remarks>
/// <para>
/// The list at the head of <c>rr:ability</c>, transcribed. Every occurrence in
/// the game — placing threat, dealing damage, playing a card, revealing an
/// encounter card, a scheme completing — is surrounded by these tiers, so this
/// is the one ordering the whole engine is expressed in.
/// </para>
/// <para>
/// The Rules Reference numbers five items and sub-numbers two of them; the
/// eight members here are that structure flattened, because the sub-items are
/// strict priorities rather than groupings — a status card's forced interrupt
/// beats an ordinary one, and a forced response beats an ordinary one.
/// </para>
/// <para>
/// The numeric values order the tiers and are not a wire format. The members
/// are the rulebook's own tiers and nothing else — no <c>Normal</c>, no
/// <c>Rule</c>, no catch-all. A tier that names nothing in <c>rr:ability</c>
/// has no rule deciding when it resolves, which is the whole point of the
/// type.
/// </para>
/// </remarks>
public enum TimingPriority
{
    /// <summary>
    /// Not timed around an occurrence: actions, resource abilities, setup
    /// abilities, "Special". These have a bold trigger but no place on
    /// <c>rr:ability</c>'s list.
    /// </summary>
    Untimed = 0,

    /// <summary>
    /// Constant abilities, delayed effects and lasting effects —
    /// <c>rr:ability.step.1</c>. Delayed and lasting effects are here rather
    /// than elsewhere because the rules put them here explicitly:
    /// <c>rr:delayed-effect.1.1</c> and <c>rr:lasting-effects.2</c>.
    /// </summary>
    Continuous = 1,

    /// <summary>Status card "Forced Interrupt" abilities — <c>rr:ability.step.2.a</c>.</summary>
    StatusForcedInterrupt = 2,

    /// <summary>"Forced Interrupt" abilities — <c>rr:ability.step.2.b</c>.</summary>
    ForcedInterrupt = 3,

    /// <summary>"Interrupt" abilities — <c>rr:ability.step.2.c</c>.</summary>
    Interrupt = 4,

    /// <summary>
    /// The occurrence itself, and the mandatory abilities that are the
    /// occurrence: "Boost" and "When Revealed" — <c>rr:ability.step.3</c>.
    /// </summary>
    Occurrence = 5,

    /// <summary>"Forced Response" abilities — <c>rr:ability.step.4.a</c>.</summary>
    ForcedResponse = 6,

    /// <summary>"Response" abilities — <c>rr:ability.step.4.b</c>.</summary>
    Response = 7,

    /// <summary>
    /// Consequential damage — <c>rr:ability.step.5</c>. After the responses,
    /// which <c>rr:consequential-damage.1</c> states again in its own words.
    /// </summary>
    ConsequentialDamage = 8,
}
