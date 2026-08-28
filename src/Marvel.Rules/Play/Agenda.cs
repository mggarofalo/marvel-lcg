using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>How far through its three parts a step has got.</summary>
/// <remarks>
/// The parts are <c>rr:ability</c>'s: an interrupt window, the occurrence, a
/// response window. A step is in exactly one of them at any moment, which is
/// what makes the whole thing resumable.
/// </remarks>
public enum Stage
{
    /// <summary>Before it happens — <c>rr:ability.step.2</c>.</summary>
    Interrupts,

    /// <summary>It happens — <c>rr:ability.step.3</c>.</summary>
    Apply,

    /// <summary>After it happened — <c>rr:ability.step.4</c>.</summary>
    Responses,
}

/// <summary>
/// One thing the game is going to do, not yet done.
/// </summary>
/// <param name="What">Which step, from <see cref="Steps"/>.</param>
/// <param name="Round">Which round it belongs to.</param>
/// <param name="Number">The Rules Reference's number for it within its phase.</param>
/// <param name="Index">Which repetition — which player, or which dealt card.</param>
/// <param name="Subject">The object id it acts on, or <c>-1</c>.</param>
/// <param name="Seat">
/// The player it concerns, or <c>-1</c> for a step that concerns nobody in
/// particular. Separate from <paramref name="Index"/>, which only has to make
/// repetitions of a step distinct: threat is placed once per round and concerns
/// no player, and reading its index as a seat would tell every card that it
/// happened to the first one.
/// </param>
/// <param name="Plan">
/// Whether this only schedules other steps. A plan is not an occurrence, so it
/// opens no windows: <c>rr:villain-phase.step.2</c> is a heading, and the
/// activations under it are the things that happen.
/// </param>
/// <param name="Character">
/// The character an attack is against, or <c>-1</c> for the attacked player's
/// identity. <c>rr:attack-enemy-activation.1.1</c>: "normally the attacked
/// character is the player's hero, but abilities can instead cause an enemy to
/// attack a player's alter-ego or <b>an ally that player controls</b>", and
/// <c>rr:attacks-against-allies.1</c> keeps the player attacked either way. So
/// this names a character and not a second seat.
/// </param>
/// <param name="Tier">
/// Which of a card's abilities suspended here, or null for a step that is not
/// an ability waiting on an answer.
/// <para>
/// Only <c>Steps.ChooseOption</c> carries one. A suspended ability is found
/// again from its card, because a step cannot hold an effect tree — and a card
/// with a choice in two of its abilities cannot be found again from the card
/// and a position alone. Infinite Hunter is the first: a "When Revealed" that
/// chooses an ally and a "Boost" that chooses between two effects.
/// </para>
/// </param>
/// <param name="Placement">
/// A threat assignment already known when the step was scheduled, or null for
/// a step whose assignment is derived when its interrupt window begins.
/// </param>
/// <param name="ActivationId">
/// The stable identity of the enemy activation this step belongs to, or -1.
/// The spelling and allocation are engine choices; the rules require only that
/// a nested activation wait for the current one to finish.
/// </param>
/// <param name="FinalStep">
/// Whether this ability is the final step of the card-defined sequence that
/// scheduled it. The rulebook defines sequences but does not choose this field's
/// spelling; it is engine data carried so a suspended Special can resume with
/// the same answer.
/// </param>
/// <param name="FinalPlayer">
/// Whether this is the last frame of an effect that resolves once for each
/// player. The chosen order is represented by the frames themselves; this flag
/// lets the card interpreter resume any outer sequence after the final frame
/// without retaining a live iterator or effect tree.
/// </param>
/// <param name="EachPlayerFrame">
/// Whether a suspended choice belongs to one player's frame of an each-player
/// effect. Together with <paramref name="FinalPlayer"/>, this tells the card
/// interpreter whether answering the choice resumes only this player's body or
/// also the outer sequence. The spelling is an engine save-format choice.
/// </param>
/// <param name="Trigger">
/// Event-stream provenance carried by an internal continuation, or empty.
/// The spelling is an engine choice rather than a Rules Reference term.
/// </param>
/// <param name="CharacterAttack">The complete queued player attack, when this step is one.</param>
/// <param name="CharacterThwart">The complete queued player thwart, when this step is one.</param>
/// <param name="PlayerAction">The accepted action and its payment choices, when this step is one.</param>
/// <param name="OccurrenceId">
/// A dynamically allocated occurrence id, or null when round/number/index name it.
/// </param>
/// <param name="SurgeGained">
/// Whether this suspended reveal ability has already gained and resolved Surge.
/// This continuation flag is an engine save-format choice; it preserves the
/// Rules Reference's one effective instance of a non-numeric keyword when the
/// ability resumes after a player choice.
/// </param>
/// <param name="Discarded">
/// Cards discarded earlier in a suspended ability, by object id. The spelling
/// is an engine save-format choice; it preserves "discarded this way" bindings
/// when the ability resumes after a player choice.
/// </param>
/// <param name="ActivatedEnemies">
/// Enemies that have already activated in the current player's step-2
/// procedure, by object id. This is engine continuation data rather than a
/// Rules Reference term: it lets the procedure re-read the engaged-minion area
/// after every activation without activating an enemy twice.
/// </param>
/// <param name="ActivationPlayers">
/// The stable player order for the current step-2 procedure. This is engine
/// continuation data: eliminating a player changes <c>World.PlayerOrder</c>,
/// but must not make a continuation mistake the next surviving player for the
/// player whose enemies have already activated.
/// </param>
public readonly record struct PhaseStep(
    string What, int Round, int Number, int Index = 0, int Subject = -1, int Seat = -1,
    bool Plan = false, int Character = -1, Timing.AbilityType? Tier = null,
    ThreatPlacement? Placement = null, int ActivationId = -1, bool FinalStep = false,
    bool FinalPlayer = false, bool EachPlayerFrame = false, string Trigger = "",
    CharacterAttack? CharacterAttack = null, CharacterThwart? CharacterThwart = null,
    PlayerAction? PlayerAction = null, int? OccurrenceId = null,
    bool SurgeGained = false, IReadOnlyList<int>? Discarded = null,
    IReadOnlyList<int>? ActivatedEnemies = null,
    IReadOnlyList<int>? ActivationPlayers = null)
{
    /// <summary>What is happening, as triggering conditions.</summary>
    /// <remarks>
    /// Usually one. The villain phase's ending is two — the phase ends and the
    /// round ends — and <c>rr:triggering-condition.2</c> is why they share one
    /// occurrence rather than getting two windows each.
    /// </remarks>
    public IReadOnlyList<string> Conditions => Steps.ConditionsOf(What);

    /// <summary>This step's occurrence, distinct from every other in the game.</summary>
    /// <remarks>
    /// <c>rr:triggering-condition.1</c> is per occurrence, so two threat
    /// placements in the same game must not share an id — the second would find
    /// every interrupt already spent.
    /// </remarks>
    public Occurrence OccurrenceOf(World world, ICardFacts facts)
    {
        int id = OccurrenceId ?? Moment.Id(Round, Number, Index);

        return What switch
        {
            Steps.PlaceThreat => VillainPhaseThreat(id, world, facts),
            Steps.SchemeThreat => SchemeThreat(id, world, facts),
            Steps.PlaceThreatEffect when Placement is { } placement =>
                Occurrence.ForThreat(id, Conditions, world, facts, placement),
            Steps.PlaceThreatEffect => throw new RulesNotImplementedException(
                "a scheduled threat placement has no placement payload"),
            Steps.Attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                Subject,
                Character >= 0 ? Character : world.Seats[Seat].IdentityCard.ObjectId,
                Seat),
            Steps.CharacterAttacks when (CharacterAttack ?? world.CharacterAttack) is { } attack =>
                Occurrence.ForAttack(
                    id,
                    Conditions,
                    world,
                    facts,
                    attack.Attacker,
                    attack.Enemy,
                    attack.Player),
            Steps.CharacterThwarts when (CharacterThwart ?? world.CharacterThwart) is { } thwart =>
                Occurrence.ForThwart(
                    id,
                    Conditions,
                    world,
                    facts,
                    thwart.Thwarter,
                    thwart.Scheme,
                    thwart.Player),
            Steps.DealAttackDamage when world.Attack is { } attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                attack.Enemy,
                attack.Target,
                attack.Player),
            Steps.EndAttack when world.Attack is { } attack => Occurrence.ForAttack(
                id,
                Conditions,
                world,
                facts,
                attack.Enemy,
                attack.Target,
                attack.Player),
            _ => new Occurrence(id, Conditions, Subject, Seat),
        };
    }

    private Occurrence VillainPhaseThreat(int id, World world, ICardFacts facts)
    {
        if (world.TheCardIn(DeckType.MainSchemesArea) is not { } scheme)
        {
            return new Occurrence(id, Conditions);
        }

        long amount = facts.PrintedValue(scheme.FaceId, "EscalationThreat", world.Players)
            + MainScheme.Acceleration(world, facts);
        if (amount <= 0)
        {
            return new Occurrence(id, Conditions, Subject: scheme.ObjectId);
        }

        return Occurrence.ForThreat(
            id,
            Conditions,
            world,
            facts,
            new ThreatPlacement(
                scheme.ObjectId, scheme.ObjectId, amount, ThreatCause.VillainPhase,
                "villain phase, place threat"));
    }

    private Occurrence SchemeThreat(int id, World world, ICardFacts facts)
    {
        if (world.TheCardIn(DeckType.MainSchemesArea) is not { } scheme
            || Subject < 0 || Subject >= world.Cards.Count)
        {
            return new Occurrence(id, []);
        }

        var enemy = world.Cards[Subject];
        long amount = StateFields.Modified(
            world, enemy, "scheme", facts, world.Players);
        if (amount <= 0)
        {
            return Occurrence.ForThreat(
                id,
                [Steps.SchemeEnds],
                world,
                facts,
                new ThreatPlacement(
                    scheme.ObjectId, enemy.ObjectId, 0, ThreatCause.EnemyScheme,
                    "scheme", Seat),
                subject: enemy.ObjectId);
        }

        return Occurrence.ForThreat(
            id,
            Conditions,
            world,
            facts,
            new ThreatPlacement(
                scheme.ObjectId, enemy.ObjectId, amount, ThreatCause.EnemyScheme,
                "scheme", Seat),
            subject: enemy.ObjectId);
    }

    /// <summary>An occurrence that needs no live attack roles, or null.</summary>
    public Occurrence? ScheduledOccurrence => What is
        Steps.Attack or Steps.CharacterAttacks or Steps.CharacterThwarts or Steps.EndAttack
            or Steps.PlaceThreat or Steps.SchemeThreat or Steps.PlaceThreatEffect
            ? null
            : new Occurrence(
                OccurrenceId ?? Moment.Id(Round, Number, Index), Conditions, Subject, Seat);
}

/// <summary>An accepted player Action, stored as data until its agenda step applies.</summary>
/// <remarks>
/// The rules do not define an engine command format. Keeping the reconstructable
/// ability address and copied input lists here is the engine's choice, and lets
/// an action survive a suspended interrupt window without retaining a call stack,
/// effect tree or session affordance handle.
/// </remarks>
public sealed record PlayerAction(
    PendingAbility Ability,
    IReadOnlyList<int> Paying,
    IReadOnlyList<int> Chosen);

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
/// Data, so it can be written to a save. The alternative is a suspended call
/// stack, which cannot be saved, cannot be diffed against a recorded step, and
/// cannot tell a client what the game is waiting for.
/// </para>
/// <para>
/// It also makes <c>rr:villain-phase</c>'s six steps <b>visible</b>. They used
/// to be the order of six method calls, which is a thing a reader has to
/// reconstruct; now they are six values that can be listed.
/// </para>
/// </remarks>
public sealed class Agenda
{
    private readonly List<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)> items = [];
    private readonly Dictionary<int, List<int>> queuedAfterActivation = [];
    private int scheduled;
    private int nextActivationId;
    private int nextPlayerActionOccurrence = -1;

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
                and not Steps.CompleteSchemeActivation),
    ];

    /// <summary>Remember gained Surge on every continuation of one revealed card.</summary>
    /// <remarks>
    /// A reveal can schedule a continuation and then resolve another printed
    /// ability before that continuation resumes. Updating every frame keeps
    /// the reveal-scoped non-numeric keyword state authoritative instead of
    /// leaving the earlier frame with a stale by-value snapshot. The flag and
    /// this propagation are engine save-format choices.
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
                    or Steps.ResolveEachPlayer;
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

    /// <summary>Put a step at the end of the list.</summary>
    /// <param name="step">What to do.</param>
    public void Add(PhaseStep step)
    {
        if (IsActivation(step))
        {
            AddActivation(step);
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

    /// <summary>
    /// Move work scheduled by the applying occurrence ahead of its response window.
    /// </summary>
    /// <remarks>
    /// An Action is not complete while an effect it scheduled still needs an
    /// answer. <see cref="Then"/> normally places child work after the current
    /// occurrence; an Action calls this after its Apply body so those children
    /// resolve before the Action reaches Responses. Work inserted with
    /// <see cref="Now(PhaseStep)"/> or <see cref="Before"/> is already ahead.
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
        if (IsActivation(step))
        {
            ThenActivation(step);
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

    /// <summary>Schedule one complete enemy activation and return its stable id.</summary>
    /// <remarks>
    /// The completion sentinel is present before the activation starts, so an
    /// activation initiated during one of its substeps can be placed after the
    /// whole activation rather than merely after that substep. This is
    /// <c>rr:activation.8</c>. The id is allocated monotonically; it is an
    /// engine wire choice rather than a Rules Reference value.
    /// </remarks>
    public int ThenActivation(PhaseStep step)
    {
        if (!IsActivation(step))
        {
            throw new ArgumentException("the step is not an enemy activation", nameof(step));
        }

        int id = nextActivationId++;
        var root = step with { ActivationId = id };
        var completion = Completion(root);
        var pair = new[]
        {
            (root, Stage.Interrupts, root.ScheduledOccurrence),
            (completion, Stage.Interrupts, completion.ScheduledOccurrence),
        };

        if (items.Count == 0)
        {
            items.AddRange(pair);
            scheduled = 0;
            return id;
        }

        int currentActivation = Current?.ActivationId ?? -1;
        if (currentActivation >= 0)
        {
            int at = CompletionIndex(currentActivation) + 1;
            if (queuedAfterActivation.TryGetValue(currentActivation, out var queued))
            {
                foreach (int queuedId in queued)
                {
                    at = Math.Max(at, CompletionIndex(queuedId) + 1);
                }
            }
            else
            {
                queuedAfterActivation[currentActivation] = queued = [];
            }

            items.InsertRange(at, pair);
            queued.Add(id);
            return id;
        }

        scheduled += pair.Length;
        items.InsertRange(Math.Min(scheduled - pair.Length + 1, items.Count), pair);
        return id;
    }

    /// <summary>
    /// Schedule a step to be taken <i>before</i> the current one happens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:interrupt.1</c>: an interrupt "resolves <b>before</b> the
    /// triggering condition". For an interrupt whose effect is itself an
    /// activation that is not enough on its own, because
    /// <c>rr:activation.8</c> would otherwise put the new activation after —
    /// "an activation initiated during another resolves after the current
    /// activation has finished resolving". Speed Demon prints the exception
    /// as a reminder: "<i>(Resolve Speed Demon's attack first.)</i>"
    /// </para>
    /// <para>
    /// The step it goes in front of keeps the stage it had reached, so the
    /// interrupt window that was open re-opens when the agenda comes back to
    /// it. That is <c>rr:interrupt.5</c> and not an accident: using an
    /// interrupt "gives each player another opportunity" to use one.
    /// </para>
    /// </remarks>
    /// <param name="step">What to do first.</param>
    public void Now(PhaseStep step)
    {
        if (IsActivation(step))
        {
            NowActivation(step);
            return;
        }

        Now([step]);
    }

    /// <summary>Schedule several steps now without reversing their order.</summary>
    public void Now(IReadOnlyList<PhaseStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        items.InsertRange(
            0,
            steps.Select(step => (step, Stage.Interrupts, step.ScheduledOccurrence)));

        // The inserted step is where `Then` now counts from, and it has
        // scheduled nothing of its own yet.
        scheduled = 0;
    }

    /// <summary>Insert a plan immediately before the item that owns an occurrence.</summary>
    /// <remarks>
    /// A nested occurrence may have been inserted in front of the occurrence
    /// that caused it. Defeat uses this boundary for damage step 8: every
    /// nested step-7 effect remains in front, while leaving play remains before
    /// the original occurrence resumes its response window at step 9.
    /// </remarks>
    public void Before(Occurrence occurrence, PhaseStep step)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        items.Insert(at, (step, Stage.Interrupts, step.ScheduledOccurrence));
    }

    private void AddActivation(PhaseStep step)
    {
        int id = nextActivationId++;
        var root = step with { ActivationId = id };
        var completion = Completion(root);
        items.Add((root, Stage.Interrupts, root.ScheduledOccurrence));
        items.Add((completion, Stage.Interrupts, completion.ScheduledOccurrence));
    }

    private void NowActivation(PhaseStep step)
    {
        int id = nextActivationId++;
        var root = step with { ActivationId = id };
        var completion = Completion(root);
        items.InsertRange(0,
        [
            (root, Stage.Interrupts, root.ScheduledOccurrence),
            (completion, Stage.Interrupts, completion.ScheduledOccurrence),
        ]);
        scheduled = 0;
    }

    private int CompletionIndex(int activationId)
    {
        int found = items.FindIndex(item =>
            item.Step.ActivationId == activationId
            && item.Step.What is Steps.CompleteAttackActivation
                or Steps.CompleteSchemeActivation);
        return found >= 0
            ? found
            : throw new InvalidOperationException(
                $"activation {activationId} has no completion sentinel");
    }

    /// <summary>Remove the unfinished steps of an activation that ended early.</summary>
    public void EndActivationEarly(int activationId, bool preserveCurrentOccurrence = true)
    {
        if (activationId < 0)
        {
            return;
        }

        // Keep the current occurrence so its response window can resolve, and
        // keep the completion sentinel so the effect that initiated the
        // activation still receives its result and can resume.
        int first = preserveCurrentOccurrence ? 1 : 0;
        for (int index = items.Count - 1; index >= first; index--)
        {
            var item = items[index];
            if (item.Step.ActivationId == activationId
                && item.Step.What is not (Steps.CompleteAttackActivation
                    or Steps.CompleteSchemeActivation))
            {
                items.RemoveAt(index);
            }
        }
    }

    private static bool IsActivation(PhaseStep step) =>
        step.What is Steps.Attack or Steps.Scheme && step.ActivationId < 0;

    private static PhaseStep Completion(PhaseStep root) => new(
        root.What == Steps.Attack
            ? Steps.CompleteAttackActivation
            : Steps.CompleteSchemeActivation,
        root.Round,
        root.Number,
        Index: root.Index,
        Subject: root.Subject,
        Seat: root.Seat,
        Plan: true,
        ActivationId: root.ActivationId);

    /// <summary>Move the current step on to its next part.</summary>
    /// <returns>False when the step is finished and has been taken off the list.</returns>
    public bool Advance()
    {
        var (step, stage, occurrence) = items[0];
        switch (stage)
        {
            case Stage.Interrupts:
                items[0] = (step, Stage.Apply, occurrence);
                return true;

            case Stage.Apply:
                items[0] = (step, Stage.Responses, occurrence);
                return true;

            default:
                items.RemoveAt(0);
                scheduled = 0;
                return false;
        }
    }

    /// <summary>Advance the item that owns <paramref name="occurrence"/>.</summary>
    /// <remarks>
    /// An applying card ability may put a nested occurrence in front of itself.
    /// Advancing by identity keeps the outer item moving to its response window
    /// without accidentally skipping the newly inserted interrupt window.
    /// </remarks>
    public bool Advance(Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        var (step, stage, found) = items[at];
        switch (stage)
        {
            case Stage.Interrupts:
                items[at] = (step, Stage.Apply, found);
                return true;
            case Stage.Apply:
                items[at] = (step, Stage.Responses, found);
                return true;
            default:
                items.RemoveAt(at);
                if (at == 0)
                {
                    scheduled = 0;
                }
                return false;
        }
    }

    /// <summary>Remove a replaced occurrence and both of its remaining windows.</summary>
    public void Cancel(Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        items.RemoveAt(at);
        if (at == 0)
        {
            scheduled = 0;
        }
    }

    /// <summary>
    /// Abandon everything outstanding.
    /// </summary>
    /// <remarks>
    /// For the end of the game. <c>rr:winning-the-game</c> and
    /// <c>rr:main-scheme-main-scheme-deck.2.1</c> both end it outright, and the
    /// rest of the villain phase does not happen.
    /// </remarks>
    public void Abandon()
    {
        items.Clear();
        scheduled = 0;
    }
}

/// <summary>The steps this engine knows how to take.</summary>
/// <remarks>
/// Named after the Rules Reference's own steps, so a divergence can be argued
/// against the published text rather than against a call graph.
/// </remarks>
public static class Steps
{
    /// <summary>The villain phase, which schedules its six steps.</summary>
    public const string VillainPhase = "VillainPhase";

    /// <summary>Step 1 — <c>rr:villain-phase.step.1</c>.</summary>
    public const string PlaceThreat = "PlaceThreat";

    /// <summary>Threat placed by a card ability or keyword.</summary>
    public const string PlaceThreatEffect = "PlaceThreatEffect";

    /// <summary>Step 2, a heading — <c>rr:villain-phase.step.2</c>.</summary>
    public const string EnemiesActivate = "EnemiesActivate";

    /// <summary>
    /// One enemy attacking one player — <c>rr:activation.1</c>,
    /// <c>rr:attack-enemy-activation</c>.
    /// </summary>
    public const string Attack = "Attack";

    /// <summary>
    /// One enemy scheming — <c>rr:activation.1</c>,
    /// <c>rr:scheme-enemy-activation</c>. Steps 1 and 2: the boost card.
    /// </summary>
    public const string Scheme = "Scheme";

    /// <summary>
    /// The initiating effect resumes after an attack activation fully resolves —
    /// <c>rr:activation.7</c>.
    /// </summary>
    public const string CompleteAttackActivation = "CompleteAttackActivation";

    /// <summary>The parallel completion sentinel for a scheme activation.</summary>
    public const string CompleteSchemeActivation = "CompleteSchemeActivation";

    /// <summary>
    /// Damage step 8, after nested step-7 abilities and before the original
    /// occurrence's response window.
    /// </summary>
    public const string FinalizeCharacterDefeat = "FinalizeCharacterDefeat";

    /// <summary>The parallel step-8 continuation for a defeated side scheme.</summary>
    public const string FinalizeSchemeDefeat = "FinalizeSchemeDefeat";

    /// <summary>
    /// Step 3 of a scheme activation —
    /// <c>rr:scheme-enemy-activation.step.3</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A step of its own because step 2 can stop and ask.</b> "Resolve each
    /// of the scheming enemy's boost cards" is step 2 and "place threat on the
    /// main scheme equal to the scheming enemy's modified SCH value" is step 3,
    /// in that order — and a <b>Boost</b> ability that offers the player a
    /// choice suspends. Resolved inline, the threat went on the scheme while
    /// the question was still on the table, and whatever the player chose
    /// arrived too late to count.
    /// </para>
    /// <para>
    /// The attack activation has the same shape:
    /// <see cref="FlipBoostCards"/> is step 3 and
    /// <see cref="CalculateAttackDamage"/> is step 4, so a boost card's
    /// question is answered between them. This is the same split one
    /// activation over.
    /// </para>
    /// <para>
    /// <b>It is also where a scheme activation ends</b>, so it carries
    /// <see cref="SchemeEnds"/> — the parallel of <see cref="AttackEnds"/> on
    /// <see cref="EndAttack"/>. "After [enemy] schemes" is a claim about the
    /// activation being over, and <c>rr:activation.6</c> is where it is over.
    /// </para>
    /// <para>
    /// It does not carry <c>WhenThreatPlaced</c>. That is
    /// <see cref="PlaceThreat"/>'s, and <see cref="PlaceThreat"/> is villain
    /// phase step 1 — a different moment. Hunting Gene Traitors answers "after
    /// resolving step one of the villain phase" and must not fire again every
    /// time the villain schemes.
    /// </para>
    /// </remarks>
    public const string SchemeThreat = "SchemeThreat";

    /// <summary>
    /// Step 1 of an attack — <c>rr:attack-enemy-activation.step.1</c>.
    /// </summary>
    public const string GiveBoostCard = "GiveBoostCard";

    /// <summary>
    /// Step 2 of an attack — <c>rr:attack-enemy-activation.step.2</c>.
    /// </summary>
    public const string DeclareDefender = "DeclareDefender";

    /// <summary>
    /// Step 3 of an attack — <c>rr:attack-enemy-activation.step.3</c>.
    /// </summary>
    public const string FlipBoostCards = "FlipBoostCards";

    /// <summary>
    /// Step 4 of an attack — <c>rr:attack-enemy-activation.step.4</c>.
    /// The calculated amount is saved on the attack for the next step.
    /// </summary>
    public const string CalculateAttackDamage = "CalculateAttackDamage";

    /// <summary>
    /// Step 5 of an attack — <c>rr:attack-enemy-activation.step.5</c>.
    /// This deals the amount fixed by <see cref="CalculateAttackDamage"/>.
    /// </summary>
    public const string DealAttackDamage = "DealAttackDamage";

    /// <summary>
    /// Move the same attack to another hero before that hero's defender window.
    /// This is an engine plan and therefore opens no timing windows of its own.
    /// </summary>
    public const string NextAttackTarget = "NextAttackTarget";

    /// <summary>
    /// Step 6 of an attack — <c>rr:attack-enemy-activation.step.6</c>.
    /// </summary>
    public const string EndAttack = "EndAttack";

    /// <summary>
    /// A hero or ally attacking an enemy —
    /// <c>rr:attack-player-ability-type</c>.
    /// </summary>
    /// <remarks>
    /// A step and not a call, for the reason every other attack is: `.step.7`
    /// and `.step.8` put abilities around it — "after [character] attacks [and
    /// damages/defeats] [an enemy/a minion]", "after [character] is attacked" —
    /// and an ability may ask the player something. A basic attack that
    /// resolved inline had nowhere to open those windows.
    /// </remarks>
    public const string CharacterAttacks = "CharacterAttacks";

    /// <summary>
    /// A hero or ally thwarting a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// A step for the reason <see cref="CharacterAttacks"/> is one, arrived at
    /// from the other end. <c>rr:thwart</c> lists no steps of its own, but
    /// <c>rr:consequential-damage.1</c> deals an ally's consequential damage
    /// "after resolving abilities that are triggered by the ally attacking
    /// <b>or thwarting</b>" — so the rules take it for granted that a thwart
    /// has abilities triggered by it, and abilities triggered by something are
    /// abilities in its windows.
    /// </remarks>
    public const string CharacterThwarts = "CharacterThwarts";

    /// <summary>
    /// An ally's consequential damage —
    /// <c>rr:attack-player-ability-type.step.9</c>.
    /// </summary>
    /// <remarks>
    /// Last of the steps an attack's resolution runs, after the forced and
    /// non-forced abilities of <c>.step.7</c> and <c>.step.8</c> —
    /// <c>rr:consequential-damage.1</c> says the same thing the other way
    /// round, "after resolving abilities that are triggered by the ally
    /// attacking or thwarting". A step of its own because those abilities are
    /// windows and a window can ask.
    /// </remarks>
    public const string AllyConsequentialDamage = "AllyConsequentialDamage";

    /// <summary>
    /// An ally's consequential damage after a thwart —
    /// <c>rr:consequential-damage.1</c>.
    /// </summary>
    /// <remarks>
    /// The same rule as <see cref="AllyConsequentialDamage"/> and a separate
    /// step only because the two differ in what they record: an ally that
    /// thwarted takes its damage under the verb "Thwart", and the event stream
    /// is how a reader tells the two apart. Which <i>field</i> was used is a
    /// third question and not this one — <c>rr:assault.2</c> makes a thwart
    /// against an assaulted scheme take the damage printed under ATK.
    /// </remarks>
    public const string AllyThwartConsequentialDamage = "AllyThwartConsequentialDamage";

    /// <summary>Step 3 — <c>rr:villain-phase.step.3</c>.</summary>
    public const string DealEncounterCards = "DealEncounterCards";

    /// <summary>
    /// Step 4 — <c>rr:villain-phase.step.4</c>. A heading, and a loop.
    /// </summary>
    /// <remarks>
    /// "Each player repeats this process in player order, <b>until no dealt
    /// encounter cards remain</b>." So this step does not hand out a list of
    /// reveals; it finds the next card, schedules that one reveal, and puts
    /// itself back on the agenda. A card revealed here that deals another card
    /// has that card revealed here too — <c>rr:deal-deal-an-encounter-card.1</c>.
    /// </remarks>
    public const string RevealEncounterCards = "RevealEncounterCards";

    /// <summary>One card being revealed — <c>rr:reveal</c>, <c>rr:villain-phase.step.4</c>.</summary>
    public const string RevealEncounterCard = "RevealEncounterCard";

    /// <summary>Discard a resolved treachery after its final nested activation.</summary>
    public const string DiscardRevealedTreachery = "DiscardRevealedTreachery";

    /// <summary>Step 5 — <c>rr:villain-phase.step.5</c>.</summary>
    /// <summary>
    /// A card ability waiting for a player to choose between its options —
    /// <c>rr:choose-option</c>.
    /// </summary>
    /// <remarks>
    /// A step rather than a call for the same reason an attack is one: the
    /// ability has to stop and ask, and an interpreter that returns a list of
    /// events has nowhere to stop. What suspends is the ability; what resumes
    /// it is the answer to this.
    /// </remarks>
    public const string ChooseOption = "ChooseOption";

    /// <summary>
    /// The first player orders the frames of an effect that resolves for each
    /// player — <c>rr:each-player.1</c>.
    /// </summary>
    public const string OrderEachPlayer = "OrderEachPlayer";

    /// <summary>One persisted player frame of an each-player card effect.</summary>
    public const string ResolveEachPlayer = "ResolveEachPlayer";

    /// <summary>
    /// A card's explicit instruction to resolve a <b>Special</b> ability —
    /// <c>rr:special</c>.
    /// </summary>
    /// <remarks>
    /// Scheduled as a plan step: resolving the Special is the work, not a new
    /// triggering condition around it. Putting it on the agenda instead of a
    /// call stack lets a choice inside the Special suspend before the next
    /// Special in the parent sequence begins.
    /// </remarks>
    public const string ResolveSpecial = "ResolveSpecial";

    /// <summary>Step 5 — <c>rr:villain-phase.step.5</c>.</summary>
    public const string PassFirstPlayerToken = "PassFirstPlayerToken";

    /// <summary>Step 6 — <c>rr:villain-phase.step.6</c>.</summary>
    public const string EndVillainPhase = "EndVillainPhase";

    /// <summary>
    /// Step 2 — <c>rr:end-of-player-phase.step.2</c>.
    /// </summary>
    /// <remarks>
    /// "Each player <b>simultaneously</b> draws up to their hand size", so one
    /// step for the table rather than one per player. Step 1 is the opposite —
    /// it is "in player order" — and lives on the turn prompt, because it is a
    /// question rather than something that happens.
    /// </remarks>
    public const string DrawToHandSize = "DrawToHandSize";

    /// <summary>
    /// Step 3 — <c>rr:end-of-player-phase.step.3</c>. Simultaneous, as step 2 is.
    /// </summary>
    public const string ReadyCards = "ReadyCards";

    /// <summary>The end of the player phase — <c>rr:end-of-player-phase</c>.</summary>
    public const string EndPlayerPhase = "EndPlayerPhase";

    /// <summary>"Whenever an enemy attacks or schemes" — <c>rr:activation</c>.</summary>
    public const string EnemyActivates = "WhenEnemyActivates";

    /// <summary>
    /// An attack begins, whoever its actor and target are.
    /// </summary>
    /// <remarks>
    /// Enemy attacks use the timing in <c>rr:attack-enemy-activation.5</c>.
    /// Character attacks use <c>rr:attack-player-ability-type.step.7</c> and
    /// <c>.step.8</c>. The occurrence's actor and target roles distinguish the
    /// printed cases without source-specific condition names.
    /// </remarks>
    public const string AttackInitiated = "WhenAttackInitiated";

    /// <summary>
    /// "When an enemy schemes" — <c>rr:scheme-enemy-activation</c>. The
    /// <i>start</i> of the activation, which is what an interrupt to it means.
    /// </summary>
    public const string EnemySchemes = "WhenEnemySchemes";

    /// <summary>
    /// "After [enemy] schemes" — the end of a scheme activation,
    /// <c>rr:activation.6</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parallel of <see cref="AttackEnds"/>, and separate from
    /// <see cref="EnemySchemes"/> for the same reason the attack keeps its two
    /// apart: <c>rr:attack-enemy-activation.5</c> puts "when [enemy name]
    /// attacks" at the moment the attack is <i>initiated</i>, before any of its
    /// steps, and <c>.step.6.a</c> is where the abilities that ask what the
    /// attack <i>did</i> live. A scheme has the same two moments and had only
    /// one name for them.
    /// </para>
    /// <para>
    /// It matters because the threat is placed in between. Prelate Armor's
    /// "<b>Forced Response</b>: After Unus schemes, give him a tough status
    /// card" resolved at the start of the activation while the two steps were
    /// one call, and nothing showed it — a tough card is a tough card whichever
    /// side of the scheme it lands on. The event order is what shows it.
    /// </para>
    /// </remarks>
    public const string SchemeEnds = "WhenSchemeEnds";

    /// <summary>"When an attack ends" — <c>rr:attack-enemy-activation.step.6</c>.</summary>
    public const string AttackEnds = "WhenAttackEnds";

    /// <summary>"When a card is revealed" — <c>rr:reveal</c>.</summary>
    public const string CardRevealed = "WhenCardRevealed";

    /// <summary>
    /// Resolving setup's abilities — <c>rr:appendix-ii-setup.step.12</c>.
    /// </summary>
    /// <remarks>
    /// <b>Not a triggering condition</b>, and deliberately absent from
    /// <see cref="EveryCondition"/>: <c>rr:setup-triggered-ability.2</c> times a
    /// "Setup" ability to a step of setup rather than to something happening,
    /// and setup is not on the agenda. This is the label its events carry, so
    /// that a board built during setup can be told apart in the stream from one
    /// built during a round.
    /// </remarks>
    public const string Setup = "Setup";

    /// <summary>
    /// A player triggering an "Action" ability on their turn —
    /// <c>rr:player-turn.5</c>.
    /// </summary>
    /// <remarks>
    /// The player chooses it directly rather than from a timing window. Once
    /// chosen it is an agenda step, because <c>rr:ability</c> puts interrupt and
    /// response windows around the occurrence and its costs and effects must
    /// remain resumable between them.
    /// </remarks>
    public const string TurnAction = "WhenActionTriggered";

    /// <summary>
    /// Damage about to be dealt to a character —
    /// <c>rr:damage.step.1</c>.
    /// </summary>
    /// <remarks>
    /// The first of the nine steps <c>rr:damage</c> lists: "abilities that
    /// trigger <i>when [character] would deal/be dealt any amount of
    /// damage</i>". This is the "be dealt" half; the dealer's half is the same
    /// step and nothing in the pool that the engine reaches uses it yet.
    /// </remarks>
    public const string DamageWouldBeDealt = "WhenDamageWouldBeDealt";

    /// <summary>A character was dealt damage.</summary>
    public const string DamageDealt = "WhenDamageDealt";

    /// <summary>
    /// A character whose remaining hit points have reached zero is about to be
    /// defeated — <c>rr:damage.step.6</c>.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="CardDefeated"/>: <c>rr:would</c> gives this
    /// condition higher priority, and an interrupt that changes the imminent
    /// defeat prevents the later condition from occurring.
    /// </remarks>
    public const string CardWouldBeDefeated = "WhenCardWouldBeDefeated";

    /// <summary>A player card has finished entering play.</summary>
    public const string CardPlayed = "WhenCardPlayed";

    /// <summary>Choose an ally to discard after exceeding the ally limit.</summary>
    public const string ChooseAllyForLimit = "ChooseAllyForLimit";

    /// <summary>Apply an ally's entry state after an ally-limit choice.</summary>
    public const string FinalizeAllyEntry = "FinalizeAllyEntry";

    /// <summary>A card finished entering play, however it got there.</summary>
    public const string CardEntersPlay = "WhenCardEntersPlay";

    /// <summary>An identity finished changing form.</summary>
    public const string FormChanged = "WhenFormChanged";

    /// <summary>A card being defeated — <c>rr:defeat</c>.</summary>
    /// <remarks>
    /// <para>
    /// A condition rather than a step, and <c>rr:triggering-condition.2</c> is
    /// why: "a single attack causing a character to both take damage and be
    /// defeated" gets "a single interrupt window and a single response window",
    /// so the defeat joins the occurrence that caused it instead of being
    /// scheduled beside it. <c>Occurrence.Also</c> is where it joins.
    /// </para>
    /// <para>
    /// <b>Reachable in a response window, and not in an interrupt one.</b> Not
    /// a gap: <c>rr:damage.step.7</c> puts "abilities that trigger <i>when
    /// [character] is defeated…</i>" after <c>.step.5</c> has placed the
    /// damage, which is past the window. So the interrupt tier is reached from
    /// inside the damage — <c>ICardAbilities.WhenCardDefeated</c> — and every
    /// ability there is forced, with nothing to offer and nothing to decline.
    /// The response tier is <c>.step.9</c>, which is the window.
    /// </para>
    /// <para>
    /// <c>rr:damage.step.6</c> is a different condition:
    /// <see cref="CardWouldBeDefeated"/>. It happens after damage is placed
    /// and before this condition, so a replacement there can prevent this one.
    /// </para>
    /// </remarks>
    public const string CardDefeated = "WhenCardDefeated";

    /// <summary>
    /// A character thwarting a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Occurrence.Subject"/> is the scheme, so a card on it answers
    /// with <c>this</c>, and <see cref="Occurrence.Player"/> is the seat
    /// thwarting, so a player card answers with <c>you</c>.
    /// </remarks>
    public const string CharacterThwartsScheme = "WhenCharacterThwarts";

    /// <summary>Threat is imminent, before prevention or replacement.</summary>
    public const string ThreatWouldBePlaced = "WhenThreatWouldBePlaced";

    /// <summary>A positive amount of threat was placed.</summary>
    public const string ThreatPlaced = "WhenThreatPlaced";

    /// <summary>Step one of the villain phase finished resolving.</summary>
    public const string VillainPhaseStepOneEnds = "WhenVillainPhaseStepOneEnds";

    private static readonly Dictionary<string, string[]> Conditions = new(StringComparer.Ordinal)
    {
        [PlaceThreat] = [ThreatWouldBePlaced],
        [PlaceThreatEffect] = [ThreatWouldBePlaced],

        // Two conditions at one moment again: an attack *is* an activation
        // (`rr:activation`, "whenever an enemy attacks or schemes, it is
        // considered to have activated"), so both are true of the same
        // occurrence and `rr:triggering-condition.2` gives them one window
        // pair between them.
        [Attack] = [EnemyActivates, AttackInitiated],
        [Scheme] = [EnemyActivates, EnemySchemes],
        [SchemeThreat] = [ThreatWouldBePlaced],
        [GiveBoostCard] = ["WhenBoostCardGiven"],
        [DeclareDefender] = ["WhenDefenderDeclared"],
        [FlipBoostCards] = ["WhenBoostCardsFlipped"],
        // Damage from an attack is imminent before this step applies. Whether
        // it was dealt is known only afterwards: Tough may prevent it, so the
        // applying step adds `DamageDealt` only when damage actually lands.
        // `rr:triggering-condition.2` still gives both one occurrence and one
        // pair of windows.
        [DealAttackDamage] = [DamageWouldBeDealt],
        [EndAttack] = [AttackEnds],
        [DealEncounterCards] = ["WhenEncounterCardsDealt"],
        [RevealEncounterCard] = [CardRevealed],
        [TurnAction] = [TurnAction],
        [CardDefeated] = [CardDefeated],
        [CharacterAttacks] = [AttackInitiated],
        [CharacterThwarts] = [CharacterThwartsScheme],
        [DamageWouldBeDealt] = [DamageWouldBeDealt],
        [CardWouldBeDefeated] = [CardWouldBeDefeated],
        // Playing a non-event card is one occurrence that both plays the card
        // and makes it enter play. `rr:triggering-condition.2` gives a single
        // occurrence that creates several triggering conditions one pair of
        // windows rather than one pair per description of the moment.
        [CardPlayed] = [CardPlayed, CardEntersPlay],
        [CardEntersPlay] = [CardEntersPlay],
        [FormChanged] = [FormChanged],
        [ChooseOption] = ["WhenOptionChosen"],
        [PassFirstPlayerToken] = ["WhenFirstPlayerTokenPassed"],

        // Two conditions at one moment, because `rr:villain-phase.step.6` is
        // titled "End of Villain Phase and Round" and both are reached there.
        // `rr:triggering-condition.2` gives them one interrupt window and one
        // response window between them.
        [EndVillainPhase] = [PhaseEnd.VillainPhaseEnds, PhaseEnd.RoundEnds],
        [EndPlayerPhase] = [PhaseEnd.PlayerPhaseEnds],
    };

    /// <summary>The triggering conditions a step creates.</summary>
    /// <param name="what">One of the step names here.</param>
    public static IReadOnlyList<string> ConditionsOf(string what) =>
        Conditions.TryGetValue(what, out var conditions) ? conditions : [what];

    /// <summary>
    /// Every triggering condition any step in this engine produces.
    /// </summary>
    /// <remarks>
    /// Derived from the table above rather than listed again, so that it cannot
    /// fall behind it. What it is for: an authored card names the condition it
    /// answers, and a card naming one nothing ever produces would sit in the
    /// dataset looking implemented and never fire. Holding the two sets against
    /// each other turns that into a failing test.
    /// </remarks>
    public static IReadOnlySet<string> EveryCondition { get; } =
        new HashSet<string>(
            Conditions.Values.SelectMany(each => each).Concat(
                // These are discovered while their occurrence applies rather
                // than promised when its step is scheduled.
                [DamageDealt, ThreatPlaced, VillainPhaseStepOneEnds, SchemeEnds]),
            StringComparer.Ordinal);
}
