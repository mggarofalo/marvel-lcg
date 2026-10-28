using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// What the game still has to do, and where in it the game is.
/// </summary>
/// <remarks>
/// <para>
/// A phase is not a call. It is a list of steps on the board, each part-way
/// through <see cref="Stage"/>, and the engine walks it until something needs a
/// player's answer. That is the only shape that lets the game stop in the middle
/// of the villain phase — which it must, because <c>rr:ability</c> puts a window
/// before and after every occurrence and any of them may hold an ability
/// somebody has to be asked about.
/// </para>
/// <para>
/// Data, so deterministic decision replay can reconstruct it exactly. The
/// alternative is a suspended call stack, which cannot survive replay, cannot
/// be diffed against a recorded step, and cannot tell a client what the game is
/// waiting for.
/// </para>
/// <para>
/// It also makes <c>rr:villain-phase</c>'s six steps <b>visible</b> as values
/// that can be listed without reconstructing an implicit call order.
/// </para>
/// </remarks>
public sealed class Agenda
{
    internal readonly List<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)> items = [];
    internal readonly Dictionary<int, List<int>> queuedAfterActivation = [];
    /// <summary>Invocation ownership, suspension and completion for this game.</summary>
    public AbilityExecutions Executions { get; }

    /// <summary>Whether a player turn has begun and its ending timing point has not occurred.</summary>
    public bool PlayerTurnOpen { get; internal set; }

    /// <summary>Scenario-owned Kang departures waiting for their phase boundary.</summary>
    public KangPhaseTransitions KangTransitions { get; } = new();

    /// <summary>Scenario-owned first Kang split awaiting a phase boundary.</summary>
    public KangFirstSplit KangSplit { get; } = new();

    /// <summary>Create an empty game-owned agenda.</summary>
    public Agenda() => Executions = new(this);

    private long nextFrameId;
    private int scheduledCount;
    private long schedulingFrame;
    internal readonly HashSet<long> scheduledFrames = [];
    internal int scheduled
    {
        get => scheduledCount;
        set
        {
            scheduledCount = value;
            if (value == 0) scheduledFrames.Clear();
        }
    }
    internal void StartProcedureScheduling(PhaseStep owner)
    {
        scheduled = 0;
        schedulingFrame = owner.FrameId;
    }

    internal void BeginScheduling()
    {
        long current = Executions.SchedulingFrame;
        if (schedulingFrame == current) return;
        scheduled = 0;
        schedulingFrame = current;
    }

    internal int nextActivationId;
    internal int nextPlayerActionOccurrence = -1;

    internal (PhaseStep Step, Stage Stage, Occurrence? Occurrence) Frame(
        PhaseStep step, Stage stage, Occurrence? occurrence, bool inheritAttackOrigin = true) =>
        (Executions.Register(step with { FrameId = checked(++nextFrameId), FrameGame = Executions.Identity,
            AttackOrigin = step.AttackOrigin ?? (inheritAttackOrigin
                && step.Operation is not (EnemyAttackOperation or EnemySchemeOperation or ActivationCompletionOperation)
                    ? Current?.AttackOrigin : null) },
            ContinuationExecution.Of(step)), stage, occurrence);

    /// <summary>Whether the game is part-way through anything.</summary>
    public bool IsBusy => items.Count > 0;

    /// <summary>How many steps are outstanding.</summary>
    public int Count => items.Count;

    /// <summary>The step being worked on.</summary>
    public PhaseStep? Current => items.Count > 0 ? items[0].Step : null;

    /// <summary>Which part of it.</summary>
    public Stage Stage => items.Count > 0 ? items[0].Stage : Stage.Apply;

    /// <summary>
    /// What is happening, as one occurrence that lasts the whole step.
    /// </summary>
    /// <remarks>
    /// <b>Made once, when the step is scheduled, and not on every read.</b>
    /// <c>rr:triggering-condition.1</c> lets each ability trigger once per
    /// occurrence, and an occurrence is what remembers which have. A fresh one
    /// per read would forget across the answer that suspended the step, and the
    /// forced interrupt that had just resolved would resolve again — and again.
    /// </remarks>
    public Occurrence? Occurrence => items.Count > 0 ? items[0].Occurrence : null;

    /// <summary>Create the current occurrence once, from the board it begins on.</summary>
    /// <remarks>
    /// Scheduling can precede an occurrence by several questions. In
    /// particular, declaring a defender changes an attack's target before its
    /// damage occurrence begins. Capturing here gets the target at the start of
    /// the interrupt window and keeps it stable for the rest of that window.
    /// </remarks>
    public Occurrence Begin(World world, ICardFacts facts)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("the agenda has no current occurrence");
        }

        var (step, stage, occurrence) = items[0];
        if (occurrence is null
            || (step.What == Steps.DealAttackDamage
                && occurrence.Actor < 0
                && world.Attack is not null))
        {
            occurrence = step.OccurrenceOf(world, facts);
        }
        items[0] = (step, stage, occurrence);
        return occurrence;
    }

    /// <summary>Every player-visible outstanding step, in resolution order.</summary>
    /// <remarks>
    /// Activation completion sentinels are internal suspension boundaries, not
    /// game occurrences or decisions, so they are intentionally absent here.
    /// </remarks>
    public IReadOnlyList<PhaseStep> Outstanding =>
    [
        .. items
            .Select(item => item.Step)
            .Where(step => step.What is not Steps.CompleteAttackActivation
                and not Steps.CompleteSchemeActivation
                and not Steps.ResumeAbility),
    ];

    /// <summary>Whether one exact occurrence owner remains on the agenda.</summary>
    public bool IsOutstanding(PhaseStep owner, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return items.Any(item => item.Step.SameFrameAs(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
    }

    /// <summary>Whether one exact occurrence remains on the agenda.</summary>
    public bool IsOutstanding(Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return items.Any(item => ReferenceEquals(item.Occurrence, occurrence));
    }

    /// <summary>Put a step at the end of the list.</summary>
    /// <param name="step">What to do.</param>
    public void Add(PhaseStep step)
    {
        if (this.IsActivation(step))
        {
            this.AddActivation(step);
            return;
        }

        items.Add(Frame(step, Stage.Interrupts, step.ScheduledOccurrence, inheritAttackOrigin: false));
    }

    /// <summary>Schedule one accepted Action with a game-unique dynamic occurrence id.</summary>
    public void AddPlayerAction(int round, PlayerAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Add(new PhaseStep(
            new PlayerActionOperation(Action: action),
            round,
            0,
            Subject: action.Ability.Card,
            Seat: action.Ability.Player
        ) { OccurrenceId = nextPlayerActionOccurrence-- });
    }

    /// <summary>Open the lifecycle occurrence for an event played inside another window.</summary>
    public void NowEventPlayed(int round, int subject, int player) =>
        this.Now(new PhaseStep(
            new LifecycleOperation(LifecycleKind.EventPlayed),
            round,
            0,
            Subject: subject,
            Seat: player
        ) { OccurrenceId = nextPlayerActionOccurrence-- });

    /// <summary>Open the paid event's initiation window before its effects resolve.</summary>
    public EventPlayInitiation NowEventPlayInitiated(int round, int subject, int player, bool? removesThreat = null)
    {
        var initiation = new EventPlayInitiation();
        this.Now(new PhaseStep(
            new LifecycleOperation(LifecycleKind.EventPlayInitiated) { EventInitiation = initiation, EventRemovesThreat = removesThreat },
            round, 0, Subject: subject, Seat: player)
        { OccurrenceId = nextPlayerActionOccurrence-- });
        return initiation;
    }

    /// <summary>
    /// Move work scheduled by the applying occurrence ahead of its response window.
    /// </summary>
    /// <remarks>
    /// An Action is not complete while an effect it scheduled still needs an
    /// answer. <see cref="Then"/> normally places child work after the current
    /// occurrence; an Action calls this after its Apply body so those children
    /// resolve before the Action reaches Responses. Work inserted with
    /// <see cref="AgendaActivationScheduling.Now(Agenda, PhaseStep)"/> or
    /// <see cref="AgendaActivationScheduling.Before"/> is already ahead.
    /// </remarks>
    public void BeforeResponses(PhaseStep owner, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (scheduled == 0)
        {
            return;
        }

        int parent = items.FindIndex(item => item.Step.SameFrameAs(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (parent < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        // Nested work can move ahead of the owner while its effect is running.
        // Track the frames enqueued by this batch rather than inferring them
        // from a contiguous slice after the owner's current position.
        // A procedure's children inherit its execution. Other invocations keep
        // their own completion and continuation ordering.
        var children = items.Where(item => scheduledFrames.Contains(item.Step.FrameId)
            && (item.Step.ExecutionOwner is null || item.Step.ExecutionOwner == owner.ExecutionOwner)
            && !item.Step.SameFrameAs(owner)).ToList();
        items.RemoveAll(item => scheduledFrames.Contains(item.Step.FrameId)
            && (item.Step.ExecutionOwner is null || item.Step.ExecutionOwner == owner.ExecutionOwner)
            && !item.Step.SameFrameAs(owner));
        parent = items.FindIndex(item => item.Step.SameFrameAs(owner));
        items.InsertRange(parent, children);
        scheduled = 0;
    }

    /// <summary>
    /// Schedule a step to be taken as soon as the current one is finished with.
    /// </summary>
    /// <remarks>
    /// After the current step's <i>response</i> window, not before it: a step
    /// that schedules another has not itself finished happening.
    /// <c>rr:villain-phase.step.3</c> deals the encounter cards and
    /// <c>.step.4</c> reveals them, in that order and not interleaved.
    /// </remarks>
    /// <param name="step">What to do next.</param>
    public void Then(PhaseStep step)
    {
        BeginScheduling();
        if (this.IsActivation(step))
        {
            this.ThenActivation(step);
            return;
        }

        scheduled += 1;
        var frame = Frame(step, Stage.Interrupts, step.ScheduledOccurrence);
        items.Insert(Math.Min(scheduled, items.Count), frame);
        scheduledFrames.Add(frame.Step.FrameId);
    }
}
