using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Owns an optional selection of ready cards to exhaust and its exhausted-count receipt.</summary>
internal static class AbilityAnyNumberExhaustion
{
    /// <summary>Ready in-play cards the selector currently names, in object-id order.</summary>
    internal static IReadOnlyList<Card> Candidates(AbilityEffect.ExhaustAnyNumber exhaust, AbilityAdmissionContext context) =>
        [.. new AbilitySelectorEvaluation(context.Expressions.Bindings).Every(exhaust.From)
            .Where(card => card.Ready && DeckTypes.IsInPlay(card.Area.Type)).OrderBy(card => card.ObjectId)];

    // "Any number" includes zero; with nothing ready no question is asked and no
    // card is exhausted. Engine choice: the authored row reads no earlier exhaustion.
    internal static AbilityStructuralTransition Ask(AbilityStructuralContext context, AbilityEffect.ExhaustAnyNumber exhaust) =>
        Candidates(exhaust, context.Admission()).Count > 0
            ? AbilityStructuralFlowExecution.AskFor(context, exhaust) : new Complete(context.Frames);

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.ExhaustAnyNumber exhaust)
    {
        var cards = Candidates(exhaust, context.Admission());
        return new([.. cards.Select(card => card.ObjectId)], 0, cards.Count);
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.ExhaustAnyNumber exhaust)
    {
        const string description = "Choose any number of these ready characters to exhaust. Confirm with none selected to exhaust none.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose characters to exhaust", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, exhaust.OperationName(), Request(context, exhaust),
                Description: description)
            { DisplayLabel = "Choose characters to exhaust", CommitLabel = "Exhaust selected characters" }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = "Choose any number of characters to exhaust",
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static AbilityStructuralTransition Answer(AbilityStructuralContext context,
        AbilityEffect.ExhaustAnyNumber exhaust, Decision answer) =>
        answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Spent.Count != 0 || !Request(context, exhaust).Allows(answer.Targets)
            ? new Unsupported("select distinct currently ready characters to exhaust")
            : new ExhaustSelectedCommand([.. answer.Targets]);

    internal static bool Commit(AbilityStructuralContext context, AbilityEffect choice, Decision answer,
        AbilityResolutionState cast)
    {
        if (Answer(context, (AbilityEffect.ExhaustAnyNumber)choice, answer) is not ExhaustSelectedCommand command)
            throw new RulesNotImplementedException("select distinct currently ready characters to exhaust");
        long exhausted = command.Targets.Count(id =>
            AbilityCardOperations.Exhaust(cast.World.Cards[id], cast.Trigger, cast.Events));
        cast.Results["exhausted"] = exhausted;
        // Exhausting is an effect that applied. In All for One the attack that follows
        // always applies too, so dropping this record changes no admitted outcome.
        if (exhausted > 0) cast.ResolveEffect();
        return true;
    }
}
