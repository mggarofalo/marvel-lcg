using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionChoices
{
    private static readonly HashSet<string> SpecialChoiceOperations =
    [
        "resolveSpecials", "chooseTopForHand", "chooseDiscardToShuffle",
    ];

    /// <inheritdoc/>
    internal static Prompt? Choosing(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, AbilityType? tier = null)
        => execution.Choosing(world, source, player, stoppedAt, tier, finalStep: false);

    /// <inheritdoc/>
    internal static Prompt? Choosing(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        var persisted = execution.ContinuationStep(world, source, stoppedAt, tier);
        var cast = ChoiceResolutionState(
            execution, world, source, player, tier, finalStep, persisted);
        execution.RestorePersisted(cast, persisted);
        execution.RestoreChoiceCursor(source, tier, persisted, cast);
        var choice = AbilityContinuationCodec.Choice(
            execution.program, source, tier, stoppedAt, persisted, execution.AdmissionContext(cast));
        return execution.DescribeChoice(source, tier, persisted, choice, cast);
    }

    private static AbilityResolutionState ChoiceResolutionState(
        AbilityResolutionExecution execution, World world, Card source, int player,
        AbilityType? tier, bool finalStep, PhaseStep? persisted) =>
        execution.Resuming(
            world, source, player, tier, finalStep,
            persisted?.AbilityOccurrence) with
        {
            EachPlayerFrame = persisted?.EachPlayerFrame ?? false,
            FinalPlayer = persisted?.FinalPlayer ?? false,
            AbilityPlayer = persisted?.AbilityPlayer ?? player,
        };

    private static void RestoreChoiceCursor(
        this AbilityResolutionExecution execution, Card source, AbilityType? tier,
        PhaseStep? persisted, AbilityResolutionState cast)
    {
        if (persisted is not
            { AbilityOrdinal: >= 0, AbilityPath: { } } current) return;
        var decoded = AbilityContinuationWireCodec.Decode(
            execution.program, source, current, tier);
        execution.RestoreContinuationCursor(cast, decoded.State);
        execution.RestoreAlteredFromFrames(cast, decoded.State.Frames);
    }

    private static Prompt? DescribeChoice(
        this AbilityResolutionExecution execution, Card source, AbilityType? tier,
        PhaseStep? persisted, AbilityEffect choice, AbilityResolutionState cast)
    {
        var structural = execution.StructuralContext(cast);
        string operation = choice.OperationName();
        if (SpecialChoiceOperations.Contains(operation))
            return AbilityStructuralExecution.DescribeSpecialChoice(structural, choice);
        if (operation is "thwartDifferentSchemes" or "legalPractice")
            return AbilityStructuralExecution.DescribeThwartChoice(
                structural, (AbilityEffect.ThwartGroup)choice);
        Prompt? prompt = operation switch
        {
            "indirectDamage" => AbilityStructuralExecution.DescribeIndirectDamage(
                structural, (AbilityEffect.IndirectDamage)choice),
            "and" => AbilityStructuralExecution.DescribeSimultaneous(
                structural, (AbilityEffect.Simultaneous)choice),
            "enemyAttacks" or "enemySchemes" =>
                AbilityStructuralExecution.DescribeActivationOrder(
                    structural, (AbilityEffect.ActivateEnemies)choice),
            "payOrEffect" or "payOrExhaust" =>
                AbilityStructuralExecution.DescribePaymentChoice(
                    structural, (AbilityEffect.PayOrEffect)choice),
            "makeTheCall" => AbilityStructuralExecution.DescribeMakeTheCall(structural),
            _ => null,
        };
        if (prompt is not null) return prompt;
        // `rr:choose-option` and `rr:choose-game-element` are separate questions.
        var described = AbilityStructuralExecution.DescribeGenericChoice(
            structural, choice, execution.ContinuationFacts(source, persisted, tier));
        _ = execution.ApplyAdmission(described.Admission, cast);
        return described.Prompt;
    }

    /// <inheritdoc/>
    internal static Prompt? Choosing(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, AbilityType? tier,
        bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        execution.Choosing(world, source, player, stoppedAt, tier, finalStep);

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Chose(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier = null)
        => execution.Chose(world, source, player, stoppedAt, input, tier, finalStep: false);

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Chose(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep)
        => execution.ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame: false, finalPlayer: false);

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Chose(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer)
        => execution.ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame, finalPlayer, eventTrigger: null);

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Chose(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string trigger)
        => execution.ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame, finalPlayer, trigger);

    internal static List<GameEvent> ChoseCore(this AbilityResolutionExecution execution,
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string? eventTrigger = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);

        var persisted = execution.ContinuationStep(world, source, stoppedAt, tier);
        var cast = AnswerResolutionState(
            execution, world, source, player, tier, finalStep, eachPlayerFrame,
            finalPlayer, eventTrigger, persisted);
        execution.RestorePersisted(cast, persisted);
        execution.RestoreChoiceCursor(source, tier, persisted, cast);
        var choice = AbilityContinuationCodec.Choice(
            execution.program, source, tier, stoppedAt, persisted, execution.AdmissionContext(cast));
        if (cast.AbilityOrdinal >= 0) cast.TrackResolution(cast.AbilityOrdinal);
        cast.At(Math.Max(0, stoppedAt - 1));
        cast.SetContinuation(persisted?.AbilityHasContinuation
            ?? HasAuthoredContinuation(execution, source, tier, stoppedAt));
        return new AbilityChoiceAnswer(
            execution, world, source, player, stoppedAt, input, tier, persisted, cast)
            .Apply(choice);
    }

    private static AbilityResolutionState AnswerResolutionState(
        AbilityResolutionExecution execution, World world, Card source, int player,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string? eventTrigger, PhaseStep? persisted)
    {
        var continuation = world.Agenda.Current is
            { What: Steps.ChooseOption, Plan: true }
            && world.Agenda.Occurrence is { } live
            && live.Is(Steps.TurnAction) ? live : null;
        return execution.Resuming(
            world, source, player, tier, finalStep,
            persisted?.AbilityOccurrence ?? continuation) with
        {
            EachPlayerFrame = eachPlayerFrame,
            FinalPlayer = finalPlayer,
            AbilityPlayer = persisted?.AbilityPlayer ?? player,
            EventTrigger = eventTrigger,
            GainedKeywords = world.Agenda.Current is
                { What: Steps.ChooseOption, SurgeGained: true }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
    }

    private static bool HasAuthoredContinuation(
        AbilityResolutionExecution execution, Card source, AbilityType? tier,
        int stoppedAt) =>
        execution.On(source).Any(ability =>
            (tier is null || ability.Trigger.Timing == tier)
            && ability.Effect.OperationName() == "seq"
            && OrderedEffects(ability.Effect).Length > stoppedAt);

    internal static AbilityContinuationFacts ContinuationFacts(this AbilityResolutionExecution execution,
        Card source, PhaseStep? step, AbilityType? tier)
    {
        if (step is not { AbilityOrdinal: >= 0, AbilityPath: not null } persisted)
            return AbilityContinuationFacts.Empty;
        return AbilityContinuationWireCodec.Decode(execution.program, source, persisted, tier).Facts;
    }

    /// <summary>Whether the source has a player-card face.</summary>
    internal static bool IsPlayerCard(this AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        execution.IsPlayerCard(cast.World.Facts, cast.Source);

    /// <summary>Whether a card face belongs to a player rather than the scenario.</summary>
    internal static bool IsPlayerCard(this AbilityResolutionExecution execution, ICardFacts facts, Card card) => AbilityCardQueries.IsPlayerCard(facts, card);

    internal static int ControllerOf(this AbilityResolutionExecution execution, World world, Card card) => AbilityCardQueries.ControllerOf(world, card);

}
