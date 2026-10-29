using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityContinuationFrames;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionChoices
{
    private static readonly System.Collections.Immutable.ImmutableArray<string> SpecialChoiceOperations =
        ["chooseCardsToHand", "resolveSpecials", "chooseTopForHand", "chooseDiscardToShuffle", "discardHandCards", "discardResourceIcons"];

    internal static void ValidateChoice(this AbilityResolutionExecution execution, World world, PhaseStep step, Decision input)
    {
        var cast = RestoreChoice(execution, world, step);
        var choice = AbilityContinuations.Choice(execution.program, cast.Source, cast.Tier,
            step.Index, step, execution.AdmissionContext(cast));
        if (choice.OperationName() == "playSharedHeroTraitPermanent")
            _ = AbilitySharedTraitPlay.Validate(execution.StructuralContext(cast), input);
    }

    internal static Prompt? Choosing(this AbilityResolutionExecution execution, World world, PhaseStep step)
    {
        var cast = RestoreChoice(execution, world, step);
        var choice = AbilityContinuations.Choice(execution.program, cast.Source, cast.Tier,
            step.Index, step, execution.AdmissionContext(cast));
        var prompt = execution.DescribeChoice(cast.Source, cast.Tier, step, choice, cast);
        return prompt is null ? null : AbilityInvocationContinuationDescription.WithContext(execution, world, step, prompt);
    }

    internal static IReadOnlyList<GameEvent> Chose(
        this AbilityResolutionExecution execution, World world, PhaseStep step, Decision input)
    {
        world.Agenda.Executions.RequireContinuation(step);
        var cast = RestoreChoice(execution, world, step);
        var choice = AbilityContinuations.Choice(execution.program, cast.Source, cast.Tier,
            step.Index, step, execution.AdmissionContext(cast));
        var prompt = execution.DescribeChoice(cast.Source, cast.Tier, step, choice, cast)
            ?? throw new InvalidOperationException("the continuation no longer asks for an answer");
        var assessment = DecisionAssessment.Assess(prompt, input);
        if (!assessment.IsAccepted) throw new ArgumentException(assessment.Reason ?? assessment.Rejection.ToString(), nameof(input));
        if (choice.OperationName() is "discardHandCards" or "discardResourceIcons")
            AbilitySelectedHandDiscardPreflight.Validate(execution.StructuralContext(cast), choice,
                execution.ContinuationFacts(cast.Source, step, cast.Tier));
        if (choice is AbilityEffect.ChooseCardsToHand group
            && AbilityGroupRetrieval.Answer(execution.StructuralContext(cast), group, input) is Unsupported rejected)
            throw new ArgumentException(rejected.Reason, nameof(input));
        if (choice is AbilityEffect.AttackAnotherEnemy)
            AbilityStagedAttack.ValidateAnswer(cast, execution.program, input);
        cast.TrackResolution(cast.AbilityOrdinal);
        cast.At(Math.Max(0, step.Index - 1));
        var events = new AbilityChoiceAnswer(execution, step.Seat, step.Index, input, step, cast).Apply(choice);
        cast.CompleteResolution();
        return events;
    }

    private static AbilityResolutionState RestoreChoice(
        AbilityResolutionExecution execution, World world, PhaseStep step)
    {
        var source = world.Cards[step.Subject];
        var decoded = Decode(execution.program, source, step, null);
        var state = decoded.State;
        var cast = AbilityResolutionLifecycle.CreateResumedState(execution, world, source, step, state);
        execution.RestorePersisted(cast, step);
        execution.RestoreContinuationCursor(cast, state);
        execution.RestoreAlteredFromFrames(cast, state.Cursor.Frames);
        return cast;
    }

    internal static Prompt? DescribeChoice(
        this AbilityResolutionExecution execution, Card source, AbilityType? tier,
        PhaseStep? persisted, AbilityEffect choice, AbilityResolutionState cast)
    {
        if (choice is AbilityEffect.Damage { Maximum: not null } damage)
            return AbilityDamageRecipients.Describe(execution.StructuralContext(cast), damage);
        if (choice is AbilityEffect.ChooseStatusRecipients recipients) return AbilityStatusRecipients.Describe(execution.StructuralContext(cast), recipients);
        if (choice is AbilityEffect.AttackAnotherEnemy staged) return AbilityStagedAttack.Describe(cast, staged, execution.program);
        var structural = execution.StructuralContext(cast);
        string operation = choice.OperationName();
        if (choice is AbilityEffect.MoveThreat move) return AbilityThreatMoveChoice.Describe(cast, move);
        if (SpecialChoiceOperations.Contains(operation))
            return AbilitySpecialChoiceDispatch.DescribeSpecialChoice(structural, choice);
        if (operation is "distributeThreat" or "thwartDifferentSchemes" or "legalPractice")
            return AbilityStructuralExecution.DescribeThwartChoice(
                structural, (AbilityEffect.ThwartGroup)choice);
        Prompt? prompt = operation switch
        {
            "inspectDiscardOrderEncounterTop" => AbilityOrderedEncounterInspection.Describe(structural,
                (AbilityEffect.InspectDiscardOrderEncounterTop)choice),
            "attack" => AbilityCountedAttackSelection.Describe(structural, (AbilityEffect.Power)choice),
            "inspectEncounterTop" => AbilityEncounterInspection.Describe(structural, (AbilityEffect.InspectEncounterTop)choice),
            "peekTop" => AbilityDeckPeek.Describe(structural),
            "indirectDamage" => AbilityStructuralExecution.DescribeIndirectDamage(
                structural, (AbilityEffect.IndirectDamage)choice),
            "and" => AbilityStructuralExecution.DescribeSimultaneous(
                structural, (AbilityEffect.Simultaneous)choice),
            "enemyAttacks" or "enemySchemes" =>
                AbilityActivationOrderChoice.DescribeActivationOrder(
                    structural, (AbilityEffect.ActivateEnemies)choice),
            "payOrEffect" or "payOrExhaust" =>
                AbilityPaymentChoiceDescription.Describe(
                    structural, (AbilityEffect.PayOrEffect)choice),
            "makeTheCall" => AbilityStructuralExecution.DescribeMakeTheCall(structural),
            "playSharedHeroTraitPermanent" => AbilitySharedTraitPlay.Describe(structural),
            _ => null,
        };
        if (prompt is not null) return prompt;
        // `rr:choose-option` and `rr:choose-game-element` are separate questions.
        var described = AbilityStructuralExecution.DescribeGenericChoice(
            structural, choice, execution.ContinuationFacts(source, persisted, tier));
        _ = execution.ApplyAdmission(described.Admission, cast);
        return described.Prompt;
    }


    internal static AbilityContinuationFacts ContinuationFacts(this AbilityResolutionExecution execution,
        Card source, PhaseStep? step, AbilityType? tier) =>
        step is { } current && StateOf(current) is not null
            ? Decode(execution.program, source, current, tier).Facts
            : AbilityContinuationFacts.Empty;

    /// <summary>Whether the source has a player-card face.</summary>
    internal static bool IsPlayerCard(this AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        execution.IsPlayerCard(cast.World.Facts, cast.Source);

    /// <summary>Whether a card face belongs to a player rather than the scenario.</summary>
    internal static bool IsPlayerCard(this AbilityResolutionExecution execution, ICardFacts facts, Card card) => AbilityCardQueries.IsPlayerCard(facts, card);

    internal static int ControllerOf(this AbilityResolutionExecution execution, World world, Card card) => AbilityCardQueries.ControllerOf(world, card);

}
