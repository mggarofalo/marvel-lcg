using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// Something happening, with its two windows around it.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:ability</c> puts an interrupt window before every occurrence and a
/// response window after it, so this is the shape of <i>everything</i> the
/// engine does: place threat, deal damage, reveal a card, end a phase. Written
/// once here rather than per step, because a step that forgot its windows would
/// look exactly like a step that had none to open.
/// </para>
/// <para>
/// Almost every one of these windows finds nothing and closes without asking
/// anybody anything — see <see cref="Offering"/>. That is why wrapping every
/// occurrence is cheap enough to be the default.
/// </para>
/// </remarks>
public static class Moment
{
    /// <summary>
    /// Open the interrupt window, do the thing, open the response window.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="abilities">What the cards are waiting to do.</param>
    /// <param name="occurrence">What is happening.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="apply">The occurrence itself — <c>rr:ability.step.3</c>.</param>
    public static void Resolve(
        World world,
        ICardAbilities abilities,
        Occurrence occurrence,
        List<GameEvent> events,
        Action apply)
    {
        ArgumentNullException.ThrowIfNull(apply);

        Window(world, abilities, occurrence, WindowKind.Interrupt, events);
        apply();
        Window(world, abilities, occurrence, WindowKind.Response, events);
    }

    /// <summary>An occurrence's id, unique within a game.</summary>
    /// <remarks>
    /// <c>rr:triggering-condition.1</c> is per occurrence, so two threat
    /// placements in the same round must not share an id or the second would
    /// find every interrupt already spent.
    /// </remarks>
    /// <param name="round">Which round.</param>
    /// <param name="step">Which step of the phase — the Rules Reference's number.</param>
    /// <param name="index">Which repetition within that step, e.g. which player.</param>
    public static int Id(int round, int step, int index) =>
        ((round * 100) + step) * 100 + index;

    /// <summary>
    /// Carry one window as far as it goes without a player's answer.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="abilities">What the cards are waiting to do.</param>
    /// <param name="occurrence">What is happening.</param>
    /// <param name="kind">Which window.</param>
    /// <param name="events">Where to record what resolved.</param>
    public static void Window(
        World world,
        ICardAbilities abilities,
        Occurrence occurrence,
        WindowKind kind,
        List<GameEvent> events)
    {
        if (Offering.Work(world, abilities, occurrence, kind, events) is not { } prompt)
        {
            return;
        }

        // A phase runs as one call, so there is nowhere to suspend to. This is
        // unreachable today: no ported card waits in any window, which
        // `CoreSetAbilities.Waiting` says in as many words. It throws rather
        // than declining on the player's behalf, because a board that is right
        // about everything except the ability nobody was offered is the
        // dangerous failure.
        throw new RulesNotImplementedException(
            $"'{prompt.Label}' must be put to player {prompt.Player} and a phase "
            + "cannot suspend mid-resolution (MARVEL-179)");
    }
}
