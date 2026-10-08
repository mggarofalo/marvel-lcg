using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Chooses the next unresolved Special against the current board.</summary>
internal static class AbilitySpecialSequence
{
    // These names are engine-chosen continuation data, scoped to one active
    // resolveSpecials node and removed when that sequence ends.
    internal const string ResolvedPrefix = "__special.resolved.";
    internal const string Repeat = "__special.repeat";

    internal static List<Card> Remaining(AbilityStructuralContext context, AbilityEffect.CardAction effect) =>
        AbilityStructuralFlowExecution.Every(effect.Selection, context)
            .Where(card => !context.Expressions.Results.TryGetValue(Key(card), out long incarnation)
                || incarnation != card.Incarnation).ToList();

    internal static string Key(Card card) => $"{ResolvedPrefix}{card.ObjectId}";

    internal static AbilityStructuralTransition Start(AbilityStructuralContext context, AbilityEffect.CardAction effect)
    {
        List<Card> remaining = Remaining(context, effect);
        return remaining.Count switch
        {
            0 => context.Expressions.Results.Keys.Any(key => key.StartsWith(ResolvedPrefix, StringComparison.Ordinal))
                ? new NextSpecialCommand(null, true) : new Complete(context.Frames),
            1 => new NextSpecialCommand(remaining[0].ObjectId, true),
            _ => AbilityStructuralFlowExecution.AskFor(context, effect),
        };
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.CardAction effect)
    {
        World world = context.Expressions.World;
        List<Card> remaining = Remaining(context, effect);
        return new Prompt(context.Player, Question.Order, TimingPriority.Untimed,
            Steps.ResolveSpecial, $"{context.SourceFace}: choose next Special ability", false,
            remaining.Select(card => new Affordance(card.ObjectId, AbilityStructuralExecution.ChooseVerb,
                card.ObjectId, context.Player, card.FaceId)
            {
                DisplayLabel = $"Resolve {world.Facts.Title(card.FaceId)} next",
                CommitLabel = $"Resolve {world.Facts.Title(card.FaceId)} next",
                Description = "Resolve this card's Special ability next. Choose any required targets as it resolves; "
                    + "choose the next Special after it finishes.",
            }).ToList())
        {
            PublicKind = PublicDecisionKind.SpecialAbilityNext,
            DisplayQuestion = "Choose the next Special ability",
            ContextCardIds = [context.Expressions.Source.ObjectId],
            Description = $"{world.Facts.Title(context.SourceFace)}: choose one remaining Special ability. "
                + "Resolve it completely before choosing again. The last remaining ability uses its final-step effect.",
        };
    }

    internal static AbilityStructuralTransition Answer(AbilityStructuralContext context,
        AbilityEffect.CardAction effect, Decision answer)
    {
        List<Card> remaining = Remaining(context, effect);
        return answer.IsDecline || answer.Targets.Count != 0
            || remaining.All(card => card.ObjectId != answer.Affordance)
            ? new Unsupported($"'{context.SourceFace}' requires one currently remaining Special ability")
            : new NextSpecialCommand(answer.Affordance, remaining.Count == 1);
    }
}
