using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>One complete, distinct selection of damaged characters before any is healed.</summary>
internal static class AbilityHealRecipients
{
    internal static AbilityTargetAdmission.TargetLegality Target(
        AbilityRecipientEffect.ChooseHealRecipients group, AbilityAdmissionContext context) =>
        Candidates(group, context).Count > 0
            ? AbilityTargetAdmission.TargetLegality.Valid : AbilityTargetAdmission.TargetLegality.Invalid;

    // rr:heal: healing removes damage, so only a damaged character can be healed.
    internal static IReadOnlyList<Card> Candidates(AbilityRecipientEffect.ChooseHealRecipients group,
        AbilityAdmissionContext context)
    {
        var world = context.Expressions.World;
        return new AbilitySelectorEvaluation(context.Expressions.Bindings).Every(group.From)
            .Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(EffectiveCards.Kind(card, world.Facts))
                && card.Damage > 0)
            .OrderBy(card => card.ObjectId).ToList();
    }

    private static TargetRequest Request(AbilityStructuralContext context, AbilityRecipientEffect.ChooseHealRecipients group)
    {
        var cards = Candidates(group, context.Admission());
        return new([.. cards.Select(card => card.ObjectId)], 0, Math.Min(group.Maximum, cards.Count))
        {
            Details = cards.ToDictionary(card => card.ObjectId, _ => $"Heal {group.Amount} damage"),
        };
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityRecipientEffect.ChooseHealRecipients group)
    {
        string description = $"Choose up to {group.Maximum} different damaged characters. Heal {group.Amount} damage from each. Confirm with none selected to heal nothing.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose characters to heal", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, group.OperationName(), Request(context, group),
                Description: description)
            { DisplayLabel = "Choose characters to heal", CommitLabel = "Heal selected characters" }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = $"Heal {group.Amount} damage from each of up to {group.Maximum} characters",
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static void Commit(AbilityStructuralContext context, AbilityResolutionState cast,
        AbilityRecipientEffect.ChooseHealRecipients group, Decision answer)
    {
        if (answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Spent.Count != 0 || !Request(context, group).Allows(answer.Targets))
            throw new ArgumentException("Select a bounded group of distinct currently damaged characters", nameof(answer));
        // rr:choose-game-element.3.1: "The same target cannot be chosen multiple times this way."
        // The engine chooses physical-id order for healing the complete selected group.
        long healed = 0;
        foreach (int id in answer.Targets.Order())
            healed += DamageRecovery.Heal(cast.World, cast.World.Facts, cast.World.Cards[id], group.Amount,
                cast.Trigger, "Heal", cast.Events);
        if (healed > 0) cast.ResolveEffect();
    }
}
