using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>How far through its three parts a step has got.</summary>
/// <remarks>
/// The parts are <c>rr:ability</c>'s: an interrupt window, the occurrence, a
/// response window. A step is in exactly one of them at any moment, which is
/// what makes the whole thing resumable.
/// </remarks>

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
    internal int scheduled;
    internal int nextActivationId;
    internal int nextPlayerActionOccurrence = -1;

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
        return items.Any(item => item.Step.Equals(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
    }

    /// <summary>Whether one exact occurrence remains on the agenda.</summary>
    public bool IsOutstanding(Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return items.Any(item => ReferenceEquals(item.Occurrence, occurrence));
    }

    /// <summary>Remember gained Surge on every continuation of one revealed card.</summary>
    /// <remarks>
    /// A reveal can schedule a continuation and then resolve another printed
    /// ability before that continuation resumes. Updating every frame keeps
    /// the reveal-scoped non-numeric keyword state authoritative instead of
    /// leaving the earlier frame with a stale by-value snapshot. The flag and
    /// this propagation are engine representation choices.
    /// </remarks>
    /// <param name="source">The revealed card whose abilities share the gain.</param>
    public void MarkSurgeGained(int source)
    {
        for (int index = 0; index < items.Count; index++)
        {
            var (step, stage, occurrence) = items[index];
            bool ownsContinuation = step.Subject == source
                && step.What is Steps.ChooseOption
                    or Steps.OrderEachPlayer
                    or Steps.ResolveEachPlayer
                    or Steps.ResumeAbility;
            ownsContinuation |= step.CharacterAttack?.Source == source
                || step.CharacterThwart?.Source == source;
            if (ownsContinuation)
            {
                items[index] = (step with
                {
                    SurgeGained = true,
                    CharacterAttack = step.CharacterAttack is { } attack
                        ? attack with { SurgeGained = true }
                        : null,
                    CharacterThwart = step.CharacterThwart is { } thwart
                        ? thwart with { SurgeGained = true }
                        : null,
                }, stage, occurrence);
            }
        }
    }

    /// <summary>Persist one per-card result on every frame of a rules procedure.</summary>
    /// <remarks>
    /// Correctness cannot depend on several steps retaining one shared
    /// collection reference. The containing occurrence identifies the frames;
    /// the dictionary spelling is an engine representation choice.
    /// </remarks>
    public void RecordProcedureAmount(Occurrence procedure, int card, long amount)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        for (int index = 0; index < items.Count; index++)
        {
            var (step, stage, occurrence) = items[index];
            if (step.ProcedureOccurrence?.Id != procedure.Id)
            {
                continue;
            }

            var amounts = step.ProcedureAmounts is { } existing
                ? new Dictionary<int, long>(existing)
                : [];
            amounts[card] = amount;
            items[index] = (step with { ProcedureAmounts = amounts }, stage, occurrence);
        }
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

        items.Add((step, Stage.Interrupts, step.ScheduledOccurrence));
    }

    /// <summary>Schedule one accepted Action with a game-unique dynamic occurrence id.</summary>
    public void AddPlayerAction(int round, PlayerAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Add(new PhaseStep(
            Steps.TurnAction,
            round,
            Number: 0,
            Subject: action.Ability.Card,
            Seat: action.Ability.Player,
            PlayerAction: action,
            OccurrenceId: nextPlayerActionOccurrence--));
    }

    /// <summary>Open the lifecycle occurrence for an event played inside another window.</summary>
    public void NowEventPlayed(int round, int subject, int player) =>
        this.Now(new PhaseStep(
            Steps.EventPlayed,
            round,
            Number: 0,
            Subject: subject,
            Seat: player,
            OccurrenceId: nextPlayerActionOccurrence--));

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
    public void BeforeResponses(Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (scheduled == 0)
        {
            return;
        }

        int parent = items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence));
        if (parent < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        int count = Math.Min(scheduled, items.Count - parent - 1);
        var children = items.GetRange(parent + 1, count);
        items.RemoveRange(parent + 1, count);
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
        if (this.IsActivation(step))
        {
            this.ThenActivation(step);
            return;
        }

        scheduled += 1;
        items.Insert(
            Math.Min(scheduled, items.Count),
            (step, Stage.Interrupts, step.ScheduledOccurrence));
    }

    /// <summary>Schedule a suspended ability continuation inside its occurrence.</summary>
    /// <remarks>
    /// A choice is an implementation suspension point, not a second game
    /// occurrence. The continuation therefore asks during <paramref name="occurrence"/>
    /// and opens no windows of its own; the owning occurrence reaches its one
    /// response window only after the continuation finishes.
    /// </remarks>
    public void ThenContinuation(PhaseStep step, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        scheduled += 1;
        items.Insert(
            Math.Min(scheduled, items.Count),
            (step with { Plan = true, OccurrenceId = occurrence.Id }, Stage.Apply, occurrence));
    }

    /// <summary>Put every child and then a continuation before its exact owner.</summary>
    /// <remarks>
    /// Several nested procedure frames may share the owner's occurrence. The
    /// step name distinguishes the original owner from those children, so the
    /// continuation remains behind every child even when more than one of them
    /// has already moved ahead of the owner.
    /// </remarks>
    public void BeforeOwnerAfterContinuations(
        Occurrence occurrence, string owner, PhaseStep continuation)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int ownerAt = items.FindIndex(item =>
            ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.What == owner);
        if (ownerAt < 0)
        {
            throw new InvalidOperationException(
                $"the '{owner}' occurrence owner is not on the agenda");
        }

        var children = items
            .Where((item, index) =>
                index != ownerAt && ReferenceEquals(item.Occurrence, occurrence))
            .ToList();
        items.RemoveAll(item =>
            ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.What != owner);
        ownerAt = items.FindIndex(item =>
            ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.What == owner);
        items.InsertRange(ownerAt, children);
        items.Insert(
            ownerAt + children.Count,
            (continuation with
            {
                Plan = true,
                OccurrenceId = occurrence.Id,
            }, Stage.Apply, occurrence));
        scheduled = 0;
    }

    /// <summary>Insert an ability continuation immediately before its exact owner.</summary>
    /// <remarks>
    /// A nested rules procedure has already moved its own frames ahead of the
    /// owner. Inserting at the owner's current index leaves those frames first
    /// while keeping pre-existing enclosing continuations behind the ability
    /// that must finish before they resume.
    /// </remarks>
    public void ContinueBeforeOwner(
        Occurrence occurrence, PhaseStep owner, PhaseStep continuation)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int ownerAt = items.FindIndex(item => item.Step.Equals(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (ownerAt < 0)
        {
            throw new InvalidOperationException("the continuation owner is not on the agenda");
        }

        items.Insert(
            ownerAt,
            (continuation with
            {
                Plan = true,
                OccurrenceId = occurrence.Id,
            }, Stage.Apply, occurrence));
        scheduled = 0;
    }
}
