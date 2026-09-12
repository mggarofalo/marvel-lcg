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
            if (EndDepartedActivation(world, step))
            {
                continue;
            }
            if (TryWorkNonApplying(
                    world, facts, abilities, events, scope, step, out Prompt? question))
            {
                if (question is not null) return question;
                continue;
            }
            Prompt? applyingQuestion = ApplyStep(world, facts, step, events);
            if (applyingQuestion is not null) return applyingQuestion;
        }

        return null;
    }

    private static bool TryWorkNonApplying(
        World world, ICardFacts facts, IWindowAbilities abilities,
        List<GameEvent> events, WindowAbilityScope scope, PhaseStep step,
        out Prompt? question)
    {
        if (step.Plan)
        {
            question = WorkPlan(world, facts, step, events);
            return true;
        }
        if (world.Agenda.Stage is Stage.Interrupts or Stage.Responses)
        {
            question = WorkWindow(world, facts, abilities, events, scope, step);
            return true;
        }
        question = null;
        return false;
    }

    private static bool EndDepartedActivation(World world, PhaseStep step)
    {
        if (step.What is Steps.EndAttack or Steps.EndSchemeEarly
            || world.Activation is not { } activation
            || step.ActivationId != activation.Id
            || DeckTypes.IsInPlay(world.Cards[activation.Enemy].Area.Type)
            || world.Agenda.Stage == Stage.Responses)
        {
            return false;
        }
        if (world.Windows.Current is not null) world.Windows.Close();
        world.Agenda.EndActivationEarly(activation.Id, preserveCurrentOccurrence: false);
        world.Agenda.Now(new PhaseStep(
            activation.Attacking ? Steps.EndAttack : Steps.EndSchemeEarly,
            step.Round, activation.Attacking ? 6 : 3,
            Index: activation.Player, Subject: activation.Enemy,
            Seat: activation.Player, ActivationId: activation.Id));
        return true;
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
        if (question is null) world.Agenda.Advance(step, occurrence);
        return question;
    }

    private static Prompt? WorkWindow(
        World world, ICardFacts facts, IWindowAbilities abilities,
        List<GameEvent> events, WindowAbilityScope scope, PhaseStep step)
    {
        var kind = world.Agenda.Stage == Stage.Interrupts
            ? WindowKind.Interrupt : WindowKind.Response;
        var occurrence = world.Agenda.Begin(world, facts);
        if (!PrepareIndirectWindow(world, step, occurrence, events)) return null;

        var status = new PriorityStatusResolution(world, facts, step, occurrence, kind, events);
        IWindowAbilities offered = step.What == Steps.PrepareIndirectAttackDamage
            ? new OptionalDamageInterrupts(abilities) : abilities;
        Prompt? question = Offering.Work(
            world, offered, occurrence, kind, events, scope, status.Resolve);
        if (question is not null) return WithAttackContext(world, facts, step, question);

        status.ObserveCancellation();
        if (status.CancelledByStatus)
        {
            CancelAttackWindow(world, step, occurrence, cancelOccurrence: true);
        }
        else if (status.CancelledOccurrence)
        {
            CancelAttackWindow(world, step, occurrence, cancelOccurrence: false);
        }
        else
        {
            world.Agenda.Advance(occurrence);
        }
        return null;
    }

    private static bool PrepareIndirectWindow(
        World world, PhaseStep step, Occurrence occurrence, List<GameEvent> events)
    {
        if (step.What != Steps.PrepareIndirectAttackDamage
            || Attack.PrepareIndirectDamage(world, step, events) > 0)
        {
            return true;
        }
        if (world.Windows.Current is not null) world.Windows.Close();
        world.Agenda.Cancel(occurrence);
        return false;
    }

    private static void CancelAttackWindow(
        World world, PhaseStep step, Occurrence occurrence, bool cancelOccurrence)
    {
        Attack.CancelPrepared(world, step.Subject);
        world.PendingAdditionalAttackPlayers = [];
        if (world.Windows.Current is not null) world.Windows.Close();
        if (cancelOccurrence) world.Agenda.Cancel(occurrence);
    }

    private static Prompt? ApplyStep(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException(
                $"an applying '{step.What}' agenda step has no occurrence");
        var healthBefore = world.Effects.CaptureCharacterHealth();
        Prompt? question = AgendaProcedures.ApplyWithWorldAbilities(world, facts, step, events);
        if (question is not null) return WithAttackContext(world, facts, step, question);
        Statuses.RemoveAfflictionsIfStalwart(world, facts, "stalwart", events);
        world.Effects.SettleLostHealth(healthBefore, step.What, events);
        if (step.What != Steps.TurnAction) world.Agenda.Advance(step, occurrence);
        if (world.IsOver) world.Agenda.Abandon();
        return null;
    }

    private sealed class PriorityStatusResolution(
        World world, ICardFacts facts, PhaseStep step, Occurrence occurrence,
        WindowKind kind, List<GameEvent> events)
    {
        public bool CancelledByStatus { get; private set; }
        public bool CancelledOccurrence { get; private set; }

        public bool Resolve()
        {
            if (!world.Agenda.IsOutstanding(step, occurrence))
            {
                CancelledOccurrence = true;
                return true;
            }
            CancelledByStatus = kind == WindowKind.Interrupt
                && step.What == Steps.Attack
                && BasicPowerStatus.Cancelled(
                    world, facts, world.Cards[step.Subject], Statuses.Stunned, events);
            if (!CancelledByStatus && kind == WindowKind.Interrupt && step.What == Steps.Attack)
            {
                Attack.Prepare(world, facts, step);
            }
            return CancelledByStatus;
        }

        public void ObserveCancellation() =>
            CancelledOccurrence |= !world.Agenda.IsOutstanding(step, occurrence);
    }

    private static Prompt WithAttackContext(
        World world, ICardFacts facts, PhaseStep step, Prompt prompt)
    {
        bool finished = world.Attack is null && step.What == Steps.EndAttack;
        EnemyAttack? attack = AttackForPrompt(world, step);
        if (attack is null || attack.Enemy < 0 || attack.Target < 0)
        {
            return prompt;
        }

        Card enemy = world.Cards[attack.Enemy];
        Card target = world.Cards[attack.Target];
        string player = AttackPlayerName(world, attack.Player);
        string stage = SequenceDescriptions.AttackStage(step.What);
        string window = AttackWindowDescription(world.Agenda.Stage);
        string situation = AttackSituation(
            world, facts, attack, enemy, target, player, finished);
        string[] attachments = AttackAttachments(world, facts, enemy);
        situation = AppendAttackContext(situation, attachments, prompt.Description);

        return prompt with
        {
            Description = $"Enemy attack · {stage} · {window}\n{situation}",
        };
    }

    private static EnemyAttack? AttackForPrompt(World world, PhaseStep step) =>
        world.Attack ?? (step.What == Steps.EndAttack ? world.FinishedAttack : null);

    private static string AttackPlayerName(World world, int player) =>
        player >= 0 && player < world.Seats.Count
            ? world.Seats[player].Name
            : $"Player {player + 1}";

    private static string AppendAttackContext(
        string situation, string[] attachments, string? description)
    {
        if (attachments.Length > 0)
        {
            situation += $" Attacker attachments: {string.Join(", ", attachments)}.";
        }
        if (!string.IsNullOrWhiteSpace(description)
            && !situation.Contains(description, StringComparison.Ordinal))
        {
            situation += $" {description}";
        }
        return situation;
    }

    private static string AttackWindowDescription(Stage stage) => stage switch
    {
        Stage.Interrupts => "Interrupt window",
        Stage.Responses => "Response window",
        _ => "Resolve step",
    };

    private static string[] AttackAttachments(World world, ICardFacts facts, Card enemy) =>
        world.Areas
            .Where(area => area.Host == enemy.ObjectId && DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Select(card => facts.Title(card.FaceId))
            .ToArray();

    private static string AttackSituation(
        World world, ICardFacts facts, EnemyAttack attack, Card enemy, Card target,
        string player, bool finished)
    {
        if (finished)
        {
            return FinishedAttackSituation(world, facts, attack, enemy, target, player);
        }
        if (attack.CalculatedDamage is { } damage)
        {
            return $"{facts.Title(enemy.FaceId)} is attacking {facts.Title(target.FaceId)} "
                + $"for {damage} damage against {player}. "
                + Damage.PreviewAttack(world, facts, enemy, enemy, target, damage);
        }
        long attackValue = StateFields.Modified(world, enemy, "attack", facts, world.Players);
        return $"{facts.Title(enemy.FaceId)} is initiating an attack against {player}. "
            + $"Target: {facts.Title(target.FaceId)}. "
            + $"ATK {attackValue} before boost icons and defense.";
    }

    private static string FinishedAttackSituation(
        World world, ICardFacts facts, EnemyAttack attack, Card enemy, Card target,
        string player)
    {
        string targetState;
        if (!DeckTypes.IsInPlay(target.Area.Type))
        {
            targetState = $"{facts.Title(target.FaceId)} was defeated.";
        }
        else
        {
            long maximum = DamagePlacement.Health(world, facts, target);
            long current = Math.Max(0, maximum - target.Damage);
            targetState = $"{facts.Title(target.FaceId)} is now at {current}/{maximum} HP.";
        }
        string damage = attack.CalculatedDamage is { } calculated
            ? $" Calculated attack damage: {calculated}."
            : string.Empty;
        return $"{facts.Title(enemy.FaceId)} finished attacking "
            + $"{facts.Title(target.FaceId)} against {player}."
            + damage
            + (attack.Damaged ? " The attack dealt damage. " : " No damage was dealt. ")
            + targetState;
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
        IWindowAbilities abilities = world.WindowAbilities;

        if (world.Windows.Current is not { } window)
        {
            AnswerAgendaStep(world, facts, asked, input, events);
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
            .Where(pair => pair.Offered.Id == input.Affordance
                && (asked.Asking != Question.Opportunity
                    || pair.Ability.Player == asked.Player
                    || pair.Ability.Player < 0))
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
        var healthBefore = world.Effects.CaptureCharacterHealth();
        events.AddRange(abilities.Resolve(
            world, window.Occurrence, ability, input.Spent, input.Targets,
            input.DefinedValues, input.Allocated));
        Statuses.RemoveAfflictionsIfStalwart(
            world, facts, "stalwart", events);
        world.Effects.SettleLostHealth(healthBefore, asked.Trigger, events);

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
        if (QuestionAdvancesByIdentity(step)
            && world.Agenda.IsOutstanding(step, occurrence))
        {
            world.Agenda.Advance(step, occurrence);
        }
        else
        {
            world.Agenda.Advance(occurrence);
        }
    }

    private static bool QuestionAdvancesByIdentity(PhaseStep step) =>
        step.What is Steps.ChooseOption
            or Steps.ChooseWouldBeDefeated
            or Steps.ChooseCardDefeatedAbility
            or Steps.ChooseRevealAbility
            or Steps.AssignIndirectAttackDamage;

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
