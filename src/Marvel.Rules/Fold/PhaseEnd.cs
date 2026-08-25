using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Fold;

/// <summary>
/// Ending a phase: the effects that expire, and the abilities that answer.
/// </summary>
/// <remarks>
/// <para>
/// The rules state this twice, in the same shape, for the two phases:
/// </para>
/// <list type="table">
///   <item>
///     <term><c>rr:villain-phase.step.6</c></term>
///     <description>
///       <i>End of Villain Phase and Round.</i> (a) effects lasting "until the
///       end of the [villain] phase" or "until the end of the round" end;
///       (b) resolve any "when/after the [villain] phase ends" or "when/after
///       the round ends" effects.
///     </description>
///   </item>
///   <item>
///     <term><c>rr:end-of-player-phase.step.4</c> and <c>.step.5</c></term>
///     <description>the same two steps for the player phase.</description>
///   </item>
/// </list>
/// <para>
/// <b>Ending a phase is an occurrence, so it has an interrupt window.</b> That
/// is not read into the rules — <c>rr:temporary.1</c> states it outright, that
/// the temporary keyword "is equivalent to the following triggered ability:
/// <i>Forced Interrupt: When the round ends, discard this card from play</i>".
/// A forced interrupt resolves before its triggering condition
/// (<c>rr:interrupt.3</c>), so a temporary card is discarded <i>before</i> the
/// effects of step 6a expire, not after.
/// </para>
/// <para>
/// The villain phase's ending is one occurrence carrying two conditions, the
/// phase ending and the round ending. <c>rr:triggering-condition.2</c> is why
/// that is one interrupt window and one response window rather than two of
/// each: an ability answering "when the round ends" gets a single turn even
/// though both conditions became true at once.
/// </para>
/// </remarks>
public static class PhaseEnd
{
    /// <summary>"When the villain phase ends", as a triggering condition.</summary>
    public const string VillainPhaseEnds = "WhenVillainPhaseEnds";

    /// <summary>"When the round ends", as a triggering condition.</summary>
    public const string RoundEnds = "WhenRoundEnds";

    /// <summary>"When the player phase ends", as a triggering condition.</summary>
    public const string PlayerPhaseEnds = "WhenPlayerPhaseEnds";

    /// <summary>
    /// End the villain phase, and with it the round —
    /// <c>rr:villain-phase.step.6</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="abilities">What the cards in play are waiting to do.</param>
    /// <param name="occurrenceId">Distinguishes this ending from the next round's.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void EndVillainPhase(
        World world, ICardAbilities abilities, int occurrenceId, List<GameEvent> events) =>
        End(world,
            abilities,
            new Occurrence(occurrenceId, [VillainPhaseEnds, RoundEnds]),
            [TimingPoints.EndOfVillainPhase, TimingPoints.EndOfRound, TimingPoints.EndOfTurn],
            events);

    /// <summary>
    /// End the player phase — <c>rr:end-of-player-phase.step.4</c> and
    /// <c>.step.5</c>.
    /// </summary>
    /// <remarks>
    /// Steps 1 to 3 of <c>rr:end-of-player-phase</c> — discarding down to hand
    /// size, drawing up to it, and readying every card — are <b>not</b> here.
    /// <c>rr:player-phase.1</c> puts them before this point, and none of them is
    /// implemented: the recorded milestone game has a full hand at every step
    /// and one player who readies nothing, so the trace cannot say when they
    /// happen. Left out rather than guessed, and named so that the gap is not
    /// mistaken for this method's business.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="abilities">What the cards in play are waiting to do.</param>
    /// <param name="occurrenceId">Distinguishes this ending from the next round's.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void EndPlayerPhase(
        World world, ICardAbilities abilities, int occurrenceId, List<GameEvent> events) =>
        End(world,
            abilities,
            new Occurrence(occurrenceId, [PlayerPhaseEnds]),
            [TimingPoints.EndOfPlayerPhase],
            events);

    private static void End(
        World world,
        ICardAbilities abilities,
        Occurrence occurrence,
        IReadOnlyList<string> expiring,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(events);

        // Before the phase ends. rr:interrupt.3, and rr:temporary.1 is the
        // keyword that lives here.
        Window(world, abilities, occurrence, WindowKind.Interrupt, events);

        // Step 6a / step 4. The phase has now ended, so everything bounded by
        // its ending is gone -- rr:lasting-effects.5.
        foreach (var timingPoint in expiring)
        {
            world.Effects.Expire(timingPoint);
        }

        // Delayed effects waiting on this moment resolve here, "before
        // responses to that point or condition may be used" -- rr:delayed-
        // effect.1 -- which is what puts this between the two windows.
        foreach (var condition in occurrence.Conditions)
        {
            var due = world.Effects.Occur(condition);
            if (due.Count > 0)
            {
                throw new RulesNotImplementedException(
                    $"{due.Count} delayed effect(s) come due at '{condition}' and "
                    + "resolving one is not implemented");
            }
        }

        // Step 6b / step 5.
        Window(world, abilities, occurrence, WindowKind.Response, events);
    }

    private static void Window(
        World world,
        ICardAbilities abilities,
        Occurrence occurrence,
        WindowKind kind,
        List<GameEvent> events)
    {
        foreach (var tier in AbilityWindow.Tiers(
            abilities.Waiting(world, occurrence, kind), kind, occurrence))
        {
            var (mandatory, optional) = AbilityWindow.Split(tier);

            if (optional.Count > 0)
            {
                // Offering it needs a prompt that can name a window, which the
                // fold's Decision cannot express yet (MARVEL-179). Throwing is
                // the alternative to silently declining on the player's behalf,
                // which would be a board that is right about everything except
                // the ability nobody was offered.
                throw new RulesNotImplementedException(
                    $"{optional.Count} optional {tier.Priority} ability(s) are waiting in the "
                    + $"{kind} window of '{string.Join("/", occurrence.Conditions)}' and the "
                    + "decision function cannot offer one (MARVEL-179)");
            }

            // rr:forced.6 -- each resolves as completely as possible before the
            // next from the same condition initiates, so they are walked one at
            // a time rather than gathered and applied together.
            foreach (var ability in mandatory)
            {
                if (mandatory.Count > 1)
                {
                    // rr:forced.5 gives this order to the first player, and
                    // nothing can ask them yet.
                    throw new RulesNotImplementedException(
                        $"{mandatory.Count} forced abilities initiate at the same moment and the "
                        + "first player chooses the order (rr:forced.5); asking is not implemented");
                }

                occurrence.Trigger(kind, ability.Card);
                events.AddRange(abilities.Resolve(world, occurrence, ability));
            }
        }
    }
}
