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
/// there. Suspended work stays game-owned; a saved ledger reconstructs it by replay.
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
    /// <param name="scope">Which cards may contribute window abilities.</param>
    /// <returns>The question the game stopped on, or null if the agenda ran out.</returns>
    public static Prompt? Work(
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events,
        WindowAbilityScope scope = WindowAbilityScope.AllCards)
    {
        ArgumentNullException.ThrowIfNull(abilities);
        world.Abilities = abilities;
        return WorkWithWorldAbilities(world, facts, events, scope);
    }

    internal static Prompt? WorkWithWorldAbilities(
        World world, ICardFacts facts, List<GameEvent> events,
        WindowAbilityScope scope = WindowAbilityScope.AllCards)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);
        IWindowAbilities abilities = world.WindowAbilities;

        while (world.Agenda.Current is { } step)
        {
            var stage = world.Agenda.Stage;
            var question = WorkIteration(world, facts, abilities, events, scope, step);
            if (question is not null) return question;
            if (world.Agenda.Current?.SameFrameAs(step) == true && world.Agenda.Stage == stage)
                throw new InvalidOperationException(
                    $"agenda frame '{step.What}' at {stage} made no progress and offered no decision");
        }

        return null;
    }

    private static Prompt? WorkIteration(
        World world, ICardFacts facts, IWindowAbilities abilities,
        List<GameEvent> events, WindowAbilityScope scope, PhaseStep step)
    {
        using var executionScope = world.Agenda.Executions.EnterProcedure(step);
        using var attackEvents = new AttackEventScope(world, events);
        return ActivationDeparture.EndDepartedActivation(world, step) ? null
            : WorkCurrentStep(world, facts, abilities, events, scope, step);
    }

    private static Prompt? WorkCurrentStep(
        World world, ICardFacts facts, IWindowAbilities abilities,
        List<GameEvent> events, WindowAbilityScope scope, PhaseStep step)
    {
        if (step.What == Steps.PlaceThreat && world.Agenda.Occurrence is null)
        {
            events.Add(new PhaseStarted("Villain") { Round = step.Round, Trigger = "WhenVillainPhaseBegins", Verb = "Phase_Started" });
            // Observe at the actual step boundary, after end-player-phase windows.
            // Unobserved scenarios allocate no additional replay frame.
            if (abilities.ObservesVillainStepOneCompletion(world))
                world.Agenda.Then(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainStepOne),
                    step.Round, step.Number, Index: step.Index + 1));
        }
        if (TryWorkNonApplying(world, facts, abilities, events, scope, step, out Prompt? question))
        {
            if (question is null) return null;
            return step.Plan ? PromptExecutionBinding.Bind(world, question)
                : PromptExecutionBinding.BindWindow(world, question);
        }
        Prompt? applyingQuestion = ApplyStep(world, facts, step, events);
        return applyingQuestion is null ? null : PromptExecutionBinding.Bind(world, applyingQuestion);
    }

    private static bool TryWorkNonApplying(
        World world, ICardFacts facts, IWindowAbilities abilities,
        List<GameEvent> events, WindowAbilityScope scope, PhaseStep step,
        out Prompt? question)
    {
        if (world.Agenda.Stage == Stage.AwaitingCompletion)
        {
            world.Agenda.Advance();
            question = null;
            return true;
        }
        if (step.Plan)
        {
            question = WorkPlan(world, facts, step, events);
            return true;
        }
        if (world.Agenda.Stage is Stage.Interrupts or Stage.Responses)
        {
            question = SequenceWindow.Work(world, facts, abilities, events, scope, step);
            return true;
        }
        question = null;
        return false;
    }


    private static Prompt? WorkPlan(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException(
                $"a planning '{step.What}' agenda step has no occurrence");
        Prompt? question = world.Agenda.Stage == Stage.Apply
            ? AgendaProcedures.ApplyWithWorldAbilities(world, facts, step, events)
            : null;
        if (question is null) world.Agenda.Applied(step, occurrence);
        return question;
    }

    private static Prompt? ApplyStep(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException(
                $"an applying '{step.What}' agenda step has no occurrence");
        var healthBefore = world.Effects.CaptureCharacterHealth();
        Prompt? question = AgendaProcedures.ApplyWithWorldAbilities(world, facts, step, events);
        if (question is not null) return AttackPromptContext.WithAttackContext(world, facts, step, question);
        Statuses.RemoveAfflictionsIfStalwart(world, facts, "stalwart", events);
        world.Effects.SettleLostHealth(healthBefore, step.What, events);
        if (step.What != Steps.TurnAction) world.Agenda.Applied(step, occurrence);
        if (world.IsOver) world.Agenda.Abandon();
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
        ArgumentNullException.ThrowIfNull(abilities);
        PromptExecutionBinding.RequireCurrent(world, asked);
        world.Abilities = abilities;
        AnswerWithWorldAbilities(world, facts, asked, input, events);
    }

    internal static void AnswerWithWorldAbilities(
        World world, ICardFacts facts, Prompt asked, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(events);
        PromptExecutionBinding.RequireCurrent(world, asked);

        using var executionScope = world.Agenda.Current is { } current
            ? world.Agenda.Executions.EnterProcedure(current) : null;
        using var attackEvents = new AttackEventScope(world, events);

        if (asked.ExecutionBinding!.Window is null)
        {
            AnswerAgendaStep(world, facts, asked, input, events);
            return;
        }

        SequenceWindow.Answer(world, facts, asked, input, events);
    }

    private static void AnswerAgendaStep(
        World world, ICardFacts facts, Prompt asked, Decision input, List<GameEvent> events)
    {
        if (world.Agenda.Current is not { } step)
        {
            throw new RulesNotImplementedException(
                $"'{asked.Label}' was answered with nothing outstanding");
        }
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("an asking agenda step has no occurrence");
        AgendaProcedures.AnswerWithWorldAbilities(world, facts, step, input, events);
        // Sequential controllers share the one Declare Defender occurrence;
        // passing does not open its response window before the choice is over.
        if (step.What == Steps.DeclareDefender && input.IsDecline
            && Attack.DeclareDefender(world, facts, world.AttackAbilities) is not null)
        {
            return;
        }
        if (QuestionAdvancesByIdentity(step)
            && world.Agenda.IsOutstanding(step, occurrence))
        {
            world.Agenda.Applied(step, occurrence);
        }
        else
        {
            world.Agenda.Advance(occurrence);
        }
    }

    private static bool QuestionAdvancesByIdentity(PhaseStep step) =>
        step.What is Steps.ChooseOption
            or Steps.ChooseAttachmentTarget
            or Steps.ChooseWouldBeDefeated
            or Steps.ChooseCardDefeatedAbility
            or Steps.ChooseRevealAbility
            or Steps.AssignIndirectAttackDamage;

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
    /// <param name="scope">Which cards may contribute window abilities.</param>
    public static void Finish(
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events,
        WindowAbilityScope scope = WindowAbilityScope.AllCards)
    {
        if (Work(world, facts, abilities, events, scope) is { } asked)
        {
            throw new RulesNotImplementedException(
                $"'{asked.Label}' must be put to player {asked.Player}, and this caller "
                + "has nobody to ask");
        }
    }
}
