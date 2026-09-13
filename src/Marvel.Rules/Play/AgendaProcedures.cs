using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Dispatches phase-neutral agenda operations to their rules procedures.</summary>
/// <remarks>
/// The engine chooses this dispatch boundary. The Rules Reference defines the
/// procedures and their order, but not the software component that routes a
/// scheduled operation to its owner. <see cref="Sequence"/> retains timing and
/// scheduling; this type applies or answers the current operation.
/// </remarks>
public static class AgendaProcedures
{
    /// <summary>Apply one agenda operation.</summary>
    /// <remarks>
    /// Returns a prompt when the step itself has something to ask, which one of
    /// them does: <c>rr:attack-enemy-activation.step.2</c> asks whether anybody
    /// defends. That is not a window — nobody is using an ability — so it is the
    /// step that stops, and the answer comes back to
    /// <see cref="Answer"/>.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">Which step.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>The question the step is waiting on, or null.</returns>
    /// <exception cref="RulesNotImplementedException">
    /// The board reached a rule this engine does not have — a minion engaged
    /// with a player, or an attack that would defeat its target.
    /// </exception>
    public static Prompt? Apply(
        World world, ICardFacts facts, ICardAbilities abilities,
        PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(abilities);
        world.Abilities = abilities;
        return ApplyWithWorldAbilities(world, facts, step, events);
    }

    internal static Prompt? ApplyWithWorldAbilities(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        return step.Operation.Procedure switch
        {
            AgendaProcedureKind.Attack => AttackProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Threat => ThreatProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Reveal => RevealProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Defeat => DefeatProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.PlayerAction =>
                PlayerActionProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.AbilityContinuation =>
                AbilityContinuationProcedure.Apply(world, step, events),
            AgendaProcedureKind.Activation => ActivationProcedure.Apply(world, facts, step),
            AgendaProcedureKind.PhaseTransition =>
                PhaseTransitionProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Lifecycle => null,
            _ => throw new RulesNotImplementedException(
                $"the agenda has no procedure for '{step.What}'"),
        };
    }

    /// <summary>Give a step the answer it stopped for.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">The step that asked.</param>
    /// <param name="input">The player's answer.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Answer(
        World world, ICardFacts facts, ICardAbilities abilities, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(abilities);
        world.Abilities = abilities;
        AnswerWithWorldAbilities(world, facts, step, input, events);
    }

    internal static void AnswerWithWorldAbilities(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        switch (step.Operation.Procedure)
        {
            case AgendaProcedureKind.Attack:
                AttackProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.Reveal:
                RevealProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.Defeat:
                DefeatProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.PlayerAction:
                PlayerActionProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.AbilityContinuation:
                AbilityContinuationProcedure.Answer(world, step, input, events);
                break;
            case AgendaProcedureKind.Activation:
                ActivationProcedure.Answer(world, step, input);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"step '{step.What}' asked nothing and cannot take an answer");
        }
    }

}
