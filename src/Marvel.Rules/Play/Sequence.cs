using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// Walking the agenda: a window, the step, a window, and on to the next.
/// </summary>
/// <remarks>
/// <para>
/// The whole of <c>rr:ability</c> in one loop. Every step on the agenda gets an
/// interrupt window before it and a response window after it, and almost every
/// one of those closes without asking anybody anything — see
/// <see cref="Offering"/>. That is why an ordinary villain phase runs from one
/// end to the other inside a single answer.
/// </para>
/// <para>
/// When a window does have something to ask, this stops and the agenda stays
/// exactly where it was: the step, which of its three parts it had reached, and
/// the open window with whose opportunity it is. The next answer picks it up
/// there. Nothing is on a call stack, so all of it survives a save.
/// </para>
/// </remarks>
public static class Sequence
{
    /// <summary>
    /// Carry the agenda as far as it goes without a player's answer.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>The question the game stopped on, or null if the agenda ran out.</returns>
    public static Prompt? Work(
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);

        while (world.Agenda.Current is { } step)
        {
            // A plan is a heading rather than something that happens, so it
            // opens no windows: `rr:villain-phase.step.2` is "Enemies
            // Activate", and the activations under it are the occurrences.
            if (step.Plan)
            {
                if (world.Agenda.Stage == Stage.Apply)
                {
                    VillainPhase.Take(world, facts, abilities, step, events);
                }

                world.Agenda.Advance();
                continue;
            }

            if (world.Agenda.Stage is Stage.Interrupts or Stage.Responses)
            {
                var kind = world.Agenda.Stage == Stage.Interrupts
                    ? WindowKind.Interrupt
                    : WindowKind.Response;

                // The agenda's occurrence and not a fresh one per read:
                // `rr:triggering-condition.1` is per occurrence, and the
                // occurrence is what remembers which abilities have used it.
                var occurrence = world.Agenda.Begin(world, facts);
                if (Offering.Work(world, abilities, occurrence, kind, events) is { } asked)
                {
                    return asked;
                }

                world.Agenda.Advance(occurrence);
                continue;
            }

            // A step may itself have a question -- declaring a defender is one
            // -- and until it is answered the step has not happened, so the
            // agenda stays where it is.
            var applying = world.Agenda.Occurrence
                ?? throw new InvalidOperationException("an applying agenda step has no occurrence");
            if (VillainPhase.Take(world, facts, abilities, step, events) is { } asking)
            {
                return asking;
            }

            world.Agenda.Advance(applying);

            if (world.IsOver)
            {
                // `rr:main-scheme-main-scheme-deck.2.1` -- the villain wins
                // outright, and the rest of the phase does not happen.
                world.Agenda.Abandon();
            }
        }

        return null;
    }

    /// <summary>
    /// Give a player's answer to the window that asked for it.
    /// </summary>
    /// <remarks>
    /// Declining moves the opportunity on to the next player
    /// (<c>rr:in-player-order</c>); taking an ability resolves it and gives
    /// everybody another opportunity, because <c>rr:interrupt.5</c> is about
    /// <i>further</i> abilities and the board has just changed.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="asked">The question that was put.</param>
    /// <param name="input">The answer.</param>
    /// <param name="events">Where to record what resolved.</param>
    public static void Answer(
        World world, ICardFacts facts, ICardAbilities abilities, Prompt asked, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(events);

        if (world.Windows.Current is not { } window)
        {
            // No window means the step itself asked. `Work` left the agenda on
            // that step's `Apply`, so answering it is what makes the step
            // happen -- and then it advances like any other.
            if (world.Agenda.Current is not { } step)
            {
                throw new RulesNotImplementedException(
                    $"'{asked.Label}' was answered with nothing outstanding");
            }

            var occurrence = world.Agenda.Occurrence
                ?? throw new InvalidOperationException("an asking agenda step has no occurrence");
            VillainPhase.Answered(world, facts, abilities, step, input, events);
            world.Agenda.Advance(occurrence);
            return;
        }

        if (input.IsDecline)
        {
            if (!asked.Cancellable)
            {
                // `rr:forced.1` -- a forced ability must resolve, so an ordering
                // question has no "none of them" answer.
                throw new RulesNotImplementedException($"'{asked.Label}' cannot be declined");
            }

            Passed(world, window.Occurrence, world.Windows.Pass());
            return;
        }

        var taken = abilities
            .Waiting(world, window.Occurrence, window.Kind)
            .Select(ability => (Ability: ability, Offered: abilities.Describe(world, ability)))
            .Where(pair => pair.Offered.Id == input.Affordance)
            .Select(pair => (PendingAbility?)pair.Ability)
            .FirstOrDefault();

        if (taken is not { } ability)
        {
            throw new RulesNotImplementedException(
                $"affordance {input.Affordance} is not on offer in '{asked.Label}'");
        }

        window.Occurrence.Trigger(window.Kind, ability.Card);

        // `rr:initiating-abilities.step.5` -- what the player spent is part of
        // the answer, not something the engine picks for them. Empty when the
        // affordance was free, which is almost all of them.
        events.AddRange(abilities.Resolve(
            world, window.Occurrence, ability, input.Spent, input.Targets));

        if (window.Occurrence.Threat is { Replaced: true })
        {
            // `rr:replacement-effect.1`: the original effect is no longer
            // imminent, so it has neither further interrupts nor responses.
            world.Windows.Close();
            world.Agenda.Cancel(window.Occurrence);
            return;
        }

        // Not a close: rr:interrupt.5 is about *further* abilities, so using one
        // gives everybody another opportunity and the step stays where it is.
        world.Windows.Used();
    }

    // Answering the last question of a window finishes that part of the step.
    // Without this the walk would find no window open, take that for "not yet
    // opened", and ask the same question again.
    private static void Passed(World world, Occurrence occurrence, bool closed)
    {
        if (closed && world.Agenda.IsBusy)
        {
            world.Agenda.Advance(occurrence);
        }
    }

    /// <summary>
    /// Run a whole phase in one go, refusing to stop.
    /// </summary>
    /// <remarks>
    /// For a caller that has no player to ask — a test, or a scenario setup.
    /// A window with a real question throws rather than being answered on the
    /// player's behalf.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Finish(
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events)
    {
        if (Work(world, facts, abilities, events) is { } asked)
        {
            throw new RulesNotImplementedException(
                $"'{asked.Label}' must be put to player {asked.Player}, and this caller "
                + "has nobody to ask");
        }
    }
}
