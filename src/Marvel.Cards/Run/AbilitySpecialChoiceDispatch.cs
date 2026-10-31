using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using static Marvel.Cards.Run.AbilityStructuralPowerExecution;

namespace Marvel.Cards.Run;

/// <summary>Routes bounded card and printed Special choices to their typed owners.</summary>
internal static class AbilitySpecialChoiceDispatch
{
    internal static Prompt DescribeSpecialChoice(
        AbilityStructuralContext context, AbilityEffect choice) => choice switch
        {
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ResolveSpecials } specials =>
                AbilitySpecialSequence.Describe(context, specials),
            AbilityEffect.ChooseTopForHand top => DescribeTopForHand(context, top),
            AbilityEffect.DiscardHandCards discard => AbilitySelectedHandDiscard.Describe(context, discard),
            AbilityEffect.DiscardResourceIcons discard => AbilityResourceIconDiscard.Describe(context, discard),
            AbilityEffect.ChooseCardsToHand group => AbilityGroupRetrieval.Describe(context, group),
            AbilityEffect.ChooseDiscardToShuffle discard => DescribeDiscardShuffle(context, discard),
            AbilityEffect.ExhaustAnyNumber exhaust => AbilityAnyNumberExhaustion.Describe(context, exhaust),
            _ => throw new InvalidOperationException($"'{context.SourceFace}' has no special prompt for '{choice.OperationName()}'"),
        };

    internal static AbilityStructuralTransition AnswerSpecialChoice(
        AbilityStructuralContext context, AbilityEffect choice, Decision answer) => choice switch
        {
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ResolveSpecials } specials =>
                AbilitySpecialSequence.Answer(context, specials, answer),
            AbilityEffect.ChooseTopForHand top => AnswerTopForHand(context, top, answer),
            AbilityEffect.DiscardHandCards discard => AbilitySelectedHandDiscard.Answer(context, discard, answer),
            AbilityEffect.DiscardResourceIcons discard => AbilityResourceIconDiscard.Answer(context, discard, answer),
            AbilityEffect.ChooseCardsToHand group => AbilityGroupRetrieval.Answer(context, group, answer),
            AbilityEffect.ChooseDiscardToShuffle discard => AnswerDiscardShuffle(context, discard, answer),
            AbilityEffect.ExhaustAnyNumber exhaust => AbilityAnyNumberExhaustion.Answer(context, exhaust, answer),
            _ => new Unsupported($"'{context.SourceFace}' has no special answer for '{choice.OperationName()}'"),
        };

}
