using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Chooses distinct enemies together for one counted attack.</summary>
internal static class AbilityCountedAttackSelection
{
    internal static bool CanInitiate(AbilityEffect.Power power, AbilityAdmission.AbilityAdmissionScope cast)
    {
        // rr:stun-stunned.5.1 permits an attack ability without valid targets
        // when stun cancels that ability after its costs have been paid.
        if (LabeledAbilities.WouldBeCancelled(cast.World, cast.World.Facts,
            AbilityInitiationPrimitives.Resolver(cast), cast.Source, [BasicPowers.AttackVerb]))
            return true;
        var candidates = Candidates(power, cast.Context);
        return Count(power, cast.Context, candidates.Count) > 0
            && cast.Context.Evaluator().Amount(((AbilityEffect.AttackDamage)power.Effect).Amount) > 0;
    }

    private static IReadOnlyList<Card> Candidates(AbilityEffect.Power power, AbilityAdmissionContext context)
    {
        var permitted = context.Selectors().Every(power.Target!).Select(card => card.ObjectId).ToHashSet();
        return [.. BasicPowers.Attackable(context.World, context.World.Facts,
                AbilityCardQueries.Resolver(context.Query))
            .Where(card => permitted.Contains(card.ObjectId)
                && AbilityProgramQueries.CanTakeDamage(context.World, context.Program, card, context.Source))];
    }

    private static int Count(AbilityEffect.Power power, AbilityAdmissionContext context, int available) =>
        checked((int)Math.Min(available, Math.Max(0, context.Evaluator().Amount(power.TargetCount!))));

    internal static AbilityStructuralTransition Start(AbilityStructuralContext context, AbilityEffect.Power power)
    {
        var admission = context.Admission();
        var candidates = Candidates(power, admission);
        // rr:choose-game-element.3: simultaneously choose as many as available,
        // to a maximum of the specified number. A disappearing last target does
        // not manufacture a target after an already paid ability resumes.
        return Count(power, admission, candidates.Count) == 0
            ? new Complete(context.Frames)
            : AbilityStructuralFlowExecution.AskFor(context, power);
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.Power power)
    {
        var admission = context.Admission();
        var candidates = Candidates(power, admission);
        int count = Count(power, admission, candidates.Count);
        string question = $"Choose {count} different enemies";
        string explanation = "All chosen enemies are attacked together.";
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose attack targets", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, "attack",
                new TargetRequest([.. candidates.Select(card => card.ObjectId)], count, count),
                Description: explanation) { DisplayLabel = question, CommitLabel = "Attack chosen enemies" }])
        {
            DisplayQuestion = question,
            Description = explanation,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static AbilityStructuralTransition Answer(
        AbilityStructuralContext context, AbilityEffect.Power power, Decision answer)
    {
        var assessment = DecisionAssessment.Assess(Describe(context, power), answer);
        if (!assessment.IsAccepted) return new Rejected(assessment.Reason ?? "Invalid attack targets");
        if (answer.Targets.Count == 0) return new Complete(context.Frames);
        var selected = answer.Targets.Select(id => context.Expressions.World.Cards[id]).ToArray();
        return AbilityStructuralPowerExecution.Power(context, power, selected[0], selected);
    }
}
