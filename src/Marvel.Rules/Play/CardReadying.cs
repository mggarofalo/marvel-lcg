using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Readies cards, opening the would-ready and readied windows when an ability observes them.</summary>
/// <remarks>
/// <para>
/// Every ready in the game goes through <see cref="Ready"/> or, for a suspendable
/// card ability, through <see cref="Observed"/>, <see cref="Commit"/> and
/// <see cref="Schedule"/>. <c>rr:interrupt.3.1</c>: "Interrupts that use the word
/// 'would' resolve before its triggering condition initiates, when that condition
/// becomes imminent." <c>rr:replacement-effect</c>: "when [triggering condition]
/// would happen, do [replacement effect] instead."
/// </para>
/// <para>
/// The ready is scheduled as its own step only when an interrupt waits for the
/// would-ready window or a response waits for the readied window. Engine
/// choice: an unobserved ready commits inline, so content without such an
/// ability keeps its events, agenda and decisions.
/// </para>
/// </remarks>
public static class CardReadying
{
    /// <summary>Engine spelling of the "when [a card] would ready" triggering condition.</summary>
    public const string WouldReady = "WhenCardWouldReady";

    /// <summary>Engine spelling of the "after [a card] readies" triggering condition.</summary>
    /// <remarks>
    /// <c>rr:triggering-condition.2</c> gives one occurrence one pair of windows:
    /// the scheduled ready's own occurrence carries this condition into its
    /// response window once a card has readied.
    /// </remarks>
    public const string Readied = "WhenCardReadied";

    /// <summary>Whether this card can be readied by this source now.</summary>
    /// <remarks>
    /// <c>rr:ready.1</c>: "If a player is instructed to ready an exhausted card,
    /// the card is returned to its ready state." <c>rr:cannot</c> makes a constant
    /// "cannot ready" absolute. A card leaving play becomes a new, ready copy
    /// (<c>rr:leaves-play.1</c>), so it is never exhausted out of play.
    /// </remarks>
    public static bool CanReady(World world, Card card, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        return !card.Ready
            && ReadinessPermissions.CanReady(world, card)
            && world.ReadinessAbilities.CanReady(world, card, source);
    }

    /// <summary>Ready the batch now, or schedule it behind its observed would-ready window.</summary>
    /// <returns>Whether the batch was scheduled rather than committed.</returns>
    public static bool Ready(World world, CardReadyBatch batch, List<GameEvent> events)
    {
        if (!Observed(world, batch))
        {
            Commit(world, batch, events, completion: null);
            return false;
        }
        Schedule(world, batch, completion: null, continuation: null);
        return true;
    }

    /// <summary>Whether an interrupt waits for this batch's would-ready window or a response for its readied window.</summary>
    /// <remarks>A read-only query: the candidate occurrences are never put on the agenda.</remarks>
    public static bool Observed(World world, CardReadyBatch batch)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(batch);
        return world.WindowAbilities.Waiting(world, Occurrence(0, batch), WindowKind.Interrupt).Count > 0
            || world.WindowAbilities.Waiting(world, new(0, [Readied], Subject: Subject(batch)) { Readying = batch },
                WindowKind.Response).Count > 0;
    }

    /// <summary>Ready every card the batch still would ready, in its resolution order.</summary>
    /// <param name="world">The board.</param>
    /// <param name="batch">The instruction.</param>
    /// <param name="events">Where to record each ready.</param>
    /// <param name="completion">The containing ability's receipt, one part per card, or null.</param>
    public static void Commit(World world, CardReadyBatch batch, List<GameEvent> events, EffectCompletion? completion)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(events);
        int start = events.Count;
        for (int part = 0; part < batch.Targets.Length; part++)
        {
            var card = world.Cards[batch.Targets[part]];
            bool ready = batch.WouldReady(world, card);
            if (ready)
            {
                batch.RecordReady(card);
                card.Refresh();
                events.Add(new FieldSet(card.ObjectId, "is_exhaust", 1, 0) { Trigger = batch.Trigger, Verb = "Ready" });
            }
            completion?.Complete(part, ready ? 1 : 0);
        }
        if (batch.Activity is { } phaseStep)
            ActivityBatches.Complete(events, start, "Ready", -1, phaseStep);
    }

    /// <summary>Schedule the batch's would-ready window and ready, then the continuation.</summary>
    public static void Schedule(World world, CardReadyBatch batch, EffectCompletion? completion, PhaseStep? continuation)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(batch);
        var current = world.Agenda.Current;
        // Engine choice: a fresh dynamic id, as a hero defense uses, keeps this
        // occurrence distinct from the step or ability that instructed it.
        var ready = new PhaseStep(new CardReadyOperation(batch, completion),
            current?.Round ?? 0, current?.Number ?? 0,
            Subject: batch.Targets.Length == 1 ? batch.Targets[0] : -1)
        { OccurrenceId = world.Agenda.nextPlayerActionOccurrence-- };
        PhaseStep[] steps = continuation is { } resume ? [ready, resume] : [ready];
        if (current is not null) world.Agenda.Now(steps);
        else foreach (var step in steps) world.Agenda.Add(step);
    }

    /// <summary>The would-ready occurrence of one batch.</summary>
    internal static Occurrence Occurrence(int id, CardReadyBatch batch) =>
        new(id, [WouldReady], Subject: Subject(batch)) { Readying = batch };

    private static int Subject(CardReadyBatch batch) => batch.Targets.Length == 1 ? batch.Targets[0] : -1;

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var operation = step.Require<CardReadyOperation>();
        int start = events.Count;
        Commit(world, operation.Batch, events, operation.Completion);
        if (operation.Batch.ReadiedFacts(world).Count > 0) world.Agenda.Occurrence?.Also(Readied);
        if (operation.Batch.Activity is not null)
        {
            // PhaseTransitionProcedure records a phase step's events in its round;
            // this ready completes that phase step.
            for (int index = start; index < events.Count; index++)
                events[index] = events[index] with { Round = step.Round };
        }
        return null;
    }
}
