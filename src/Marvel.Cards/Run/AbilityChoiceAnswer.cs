using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

internal sealed class AbilityChoiceAnswer(
    AbilityResolutionExecution execution, int player, int stoppedAt,
    Decision input, PhaseStep? persisted, AbilityResolutionState cast)
{
    private static readonly ImmutableArray<string> SpecialOperations =
    [
        "chooseCardsToHand", "resolveSpecials", "chooseTopForHand", "chooseDiscardToShuffle", "discardHandCards", "discardResourceIcons",
        "peekTop", "inspectEncounterTop", "inspectDiscardOrderEncounterTop",
    ];

    internal List<Marvel.Rules.Events.GameEvent> Apply(AbilityEffect choice)
    {
        string operation = choice.OperationName();
        if (choice is AbilityEffect.MoveThreat move)
        {
            AbilityThreatMoveChoice.Answer(execution, cast, move, input);
            return cast.Events;
        }
        if (operation is "and" or "enemyAttacks" or "enemySchemes")
            return ApplyStructural(choice);
        if (SpecialOperations.Contains(operation)) return ApplySpecial(choice);
        if (operation is "payOrEffect" or "payOrExhaust")
            return ApplyPayment(choice);
        if (operation is "distributeThreat" or "thwartDifferentSchemes" or "legalPractice")
            return ApplyThwartChoice(choice);
        if (operation == "makeTheCall") return ApplyMakeTheCall(choice);
        if (operation == "indirectDamage") return ApplyIndirectDamage(choice);
        if (operation == "attack") return ApplyCountedAttack((AbilityEffect.Power)choice);
        return ApplyResumableEffectChoice(choice);
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyStructural(AbilityEffect choice)
    {
        bool outerContinuation = cast.HasContinuation;
        AbilityStructuralTransition answer = choice.OperationName() == "and"
            ? AbilityStructuralExecution.AnswerSimultaneous(
                execution.StructuralContext(cast),
                (AbilityEffect.Simultaneous)choice, input)
            : AbilityActivationOrderChoice.AnswerActivationOrder(
                execution.StructuralContext(cast),
                (AbilityEffect.ActivateEnemies)choice, input);
        execution.ApplyStructuralDecision(answer, cast);
        if (cast.Suspended) return cast.Events;
        if (choice.OperationName() == "and") cast.SetContinuation(outerContinuation);
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplySpecial(AbilityEffect choice) =>
        choice.OperationName() switch
        {
            "chooseCardsToHand" => ApplyGroupRetrieval((AbilityEffect.ChooseCardsToHand)choice),
            "inspectDiscardOrderEncounterTop" => AbilityOrderedEncounterInspection.Answer(execution, (AbilityEffect.InspectDiscardOrderEncounterTop)choice, cast, input) ? Continue() : cast.Events,
            "peekTop" => AbilityDeckPeek.Answer(execution, choice, cast, input) ? Continue() : cast.Events,
            "inspectEncounterTop" => ApplyInspection((AbilityEffect.InspectEncounterTop)choice),
            "resolveSpecials" => ApplyResolveSpecials(choice),
            "chooseTopForHand" => ApplyChooseTopForHand(choice),
            "chooseDiscardToShuffle" => ApplyChooseDiscardToShuffle(choice),
            "discardHandCards" or "discardResourceIcons" => ApplySelectedHandDiscard(choice),
            _ => throw new InvalidOperationException("Unknown special choice"),
        };

    private List<Marvel.Rules.Events.GameEvent> ApplyGroupRetrieval(AbilityEffect.ChooseCardsToHand choice)
    {
        var answer = AbilityGroupRetrieval.Answer(execution.StructuralContext(cast), choice, input);
        ThrowIfUnsupported(answer);
        var targets = ((RetrieveCardsCommand)answer).Targets;
        AbilityGroupRetrieval.Commit(cast, targets);
        if (targets.Length > 0) cast.ResolveEffect();
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyInspection(AbilityEffect.InspectEncounterTop inspection)
    {
        AbilityEncounterInspection.Answer(cast, inspection);
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyResolveSpecials(AbilityEffect choice)
    {
        var answer = AbilitySpecialChoiceDispatch.AnswerSpecialChoice(
            execution.StructuralContext(cast), choice, input);
        ThrowIfUnsupported(answer);
        execution.ApplyStructuralDecision(answer, cast);
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyPayment(AbilityEffect choice)
    {
        var answer = AbilityStructuralExecution.AnswerPaymentChoice(
            execution.StructuralContext(cast), (AbilityEffect.PayOrEffect)choice, input);
        ThrowIfUnsupported(answer);
        if (((PayOrCommand)answer).Pay)
        {
            var requirement = (AbilityEffect.PayOrEffect)choice;
            var owner = cast.World.Agenda.Current;
            var occurrence = cast.World.Agenda.Occurrence;
            var payment = AbilityResourcePayment.Spend(cast.World, execution.resourceAbilities,
                new(cast.Source, player, input.Spent, requirement.Amount, requirement.Resources), cast.Events);
            if (!payment.IsComplete)
            {
                cast.PendingPayment = new AbilityChoicePayment(payment);
                execution.SuspendAfterProcedure(choice, cast, owner, occurrence);
                return cast.Events;
            }
            cast.ResolveEffect();
        }
        else if (input.Affordance == 1)
        {
            execution.RunChild(
                EffectFollowing(choice), new ChoiceOtherwiseFrame(), cast);
            if (cast.Suspended) return cast.Events;
        }
        else
        {
            string reason = choice.OperationName() == "payOrExhaust"
                ? $"'{cast.Source.FaceId}' offers spend or exhaust, not option {input.Affordance}"
                : $"'{cast.Source.FaceId}' did not offer option {input.Affordance}";
            throw new RulesNotImplementedException(reason);
        }
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyChooseTopForHand(AbilityEffect choice)
    {
        var answer = AbilitySpecialChoiceDispatch.AnswerSpecialChoice(
            execution.StructuralContext(cast), choice, input);
        ThrowIfUnsupported(answer);
        var command = answer as ChooseTopForHandCommand
            ?? throw new InvalidOperationException(
                "Structural owner did not return top-card selection");
        AbilityCardStateExecution.ChooseTopForHand(
            command.Top.Select(id => cast.World.Cards[id]).ToList(),
            cast.World.Cards[command.Selected], CardStateContext());
        cast.ResolveEffect();
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyChooseDiscardToShuffle(
        AbilityEffect choice)
    {
        var answer = AbilitySpecialChoiceDispatch.AnswerSpecialChoice(
            execution.StructuralContext(cast), choice, input);
        ThrowIfUnsupported(answer);
        var command = answer as ShuffleDiscardCommand
            ?? throw new InvalidOperationException(
                "Structural owner did not return discard selection");
        AbilityCardStateExecution.ShuffleDiscardIntoDeck(
            command.Targets.Select(id => cast.World.Cards[id]).ToList(),
            CardStateContext());
        cast.ResolveEffect();
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplySelectedHandDiscard(AbilityEffect choice)
    {
        var answer = AbilitySpecialChoiceDispatch.AnswerSpecialChoice(execution.StructuralContext(cast), choice, input);
        ThrowIfUnsupported(answer);
        AbilitySelectedHandDiscard.Commit(cast, ((DiscardSelectedHandCardsCommand)answer).Targets);
        cast.ResolveEffect();
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyThwartChoice(AbilityEffect choice) =>
        choice.OperationName() == "legalPractice"
            ? ApplyLegalPractice((AbilityEffect.ThwartGroup)choice)
            : ApplyDifferentSchemes((AbilityEffect.ThwartGroup)choice);

    private List<Marvel.Rules.Events.GameEvent> ApplyDifferentSchemes(
        AbilityEffect.ThwartGroup choice)
    {
        var command = AnswerThwartChoice(choice);
        var selected = command.Resolving.Select(id => cast.World.Cards[id]).ToList();
        // rr:then: only the first selected scheme belongs to the pre-then effect.
        cast.Choose(cast.World.Cards[command.Scheme]);
        execution.ApplyStructuralDecision(AbilityStructuralPowerExecution.Power(
            execution.StructuralContext(cast), choice.Thwart,
            cast.World.Cards[command.Scheme], selected, -1), cast);
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyLegalPractice(
        AbilityEffect.ThwartGroup choice)
    {
        var command = AnswerThwartChoice(choice);
        var scheme = cast.World.Cards[command.Scheme];
        AbilityCardStateExecution.DiscardCards(
            command.Discard.Select(id => cast.World.Cards[id]).ToList(),
            CardPlay.Verb, CardStateContext());
        cast.ResolveEffect();
        cast.Choose(scheme);
        execution.ApplyStructuralDecision(AbilityStructuralPowerExecution.Power(
            execution.StructuralContext(cast), choice.Thwart,
            scheme, [scheme], command.PowerAmount), cast);
        return Continue();
    }

    private ThwartSelectionCommand AnswerThwartChoice(
        AbilityEffect.ThwartGroup choice)
    {
        var answer = AbilityStructuralExecution.AnswerThwartChoice(
            execution.StructuralContext(cast), choice, input);
        ThrowIfUnsupported(answer);
        return (ThwartSelectionCommand)answer;
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyMakeTheCall(AbilityEffect choice)
    {
        var answer = AbilityStructuralExecution.AnswerMakeTheCall(
            execution.StructuralContext(cast), input);
        ThrowIfUnsupported(answer);
        var ally = cast.World.Cards[((MakeTheCallCommand)answer).Ally];
        long cost = Resources.Cost(ally.FaceId, cast.World.Facts, cast.World.Players) ?? 0;
        var owner = cast.World.Agenda.Current;
        var occurrence = cast.World.Agenda.Occurrence;
        var payment = CardPlay.Spend(
            cast.World, cast.World.Facts, execution.resourceAbilities,
            [cast.World.Seats[player].Hand], input.Spent, cost,
            Resources.Required(cast.World, ally, cast.World.Facts), cast.Source.ObjectId,
            player, cast.Events, payingFor: ally);
        if (!payment.IsComplete)
        {
            cast.PendingPayment = new AbilityChoicePayment(payment, ally);
            execution.SuspendAfterProcedure(choice, cast, owner, occurrence);
            return cast.Events;
        }
        CardPlay.PutAllyIntoPlay(
            cast.World, cast.World.Facts, execution.cardPlayAbilities, ally, player,
            cast.Trigger, cast.Events);
        cast.ResolveEffect();
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyIndirectDamage(AbilityEffect choice)
    {
        var answer = AbilityStructuralExecution.AnswerIndirectDamage(
            execution.StructuralContext(cast),
            (AbilityEffect.IndirectDamage)choice, input);
        ThrowIfUnsupported(answer);
        execution.Resolve(
            choice, cast, ((AssignedDamageCommand)answer).Assigned.ToDictionary());
        return Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyResumableEffectChoice(AbilityEffect choice)
    {
        if (choice is AbilityEffect.AttackAnotherEnemy staged)
            AbilityStagedAttack.Answer(execution, cast, staged, input);
        else
            execution.ApplyStructuralDecision(
                AbilityStructuralFlowExecution.AnswerGenericChoice(
                    execution.StructuralContext(cast), choice,
                    execution.ContinuationFacts(cast.Source, persisted, cast.Tier), input), cast);
        return cast.Suspended ? cast.Events : Continue();
    }

    private List<Marvel.Rules.Events.GameEvent> ApplyCountedAttack(AbilityEffect.Power choice)
    {
        execution.ApplyStructuralDecision(AbilityCountedAttackSelection.Answer(
            execution.StructuralContext(cast), choice, input), cast);
        return cast.Suspended ? cast.Events : Continue();
    }

    private AbilityCardStateContext CardStateContext() =>
        new(cast.ExpressionContext(), cast.Trigger, cast.Events,
            execution.cardPlayAbilities, execution.readinessAbilities,
            new AbilityCardStateResult());

    private List<Marvel.Rules.Events.GameEvent> Continue() =>
        execution.Continue(cast.Source, cast, stoppedAt);

    private static void ThrowIfUnsupported(AbilityStructuralTransition answer)
    {
        if (answer is Unsupported unsupported)
            throw new RulesNotImplementedException(unsupported.Reason);
    }
}
