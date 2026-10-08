using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityStructuralQueries;

namespace Marvel.Cards.Run;

/// <summary>Visibility-safe meaning and candidates for an admitted generic choice.</summary>
internal static class AbilityChoicePromptDescription
{
    internal static AbilityStructuralPrompt DescribeChoice(
        AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation)
    {
        var evidence = NewEvidence();
        bool cards = choice is AbilityEffect.ChooseCard;
        bool concealedCards = choice is AbilityEffect.ChooseCard exposureChoice
            && InspectsConcealedPile(exposureChoice.From);
        IEnumerable<Affordance> affordances = choice switch
        {
            AbilityEffect.ChooseCard selecting => CardChoices(context, selecting, continuation, evidence),
            AbilityEffect.Choose options => OptionChoices(context, options, continuation, evidence),
            _ => throw new InvalidOperationException($"'{context.SourceFace}' does not contain a generic choice"),
        };
        var offered = affordances.ToList();
        if (offered.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"'{context.SourceFace}' requires a choice and has no legal option");
        }
        var prompt = new Prompt(
            context.Player, cards ? Question.Element : Question.Option,
            TimingPriority.Untimed, Steps.CardRevealed,
            $"{context.SourceFace}: choose {(cards ? "a card" : "an option")}",
            Cancellable: false, offered) {
            ContextCardIds = [context.Expressions.Source.ObjectId],
            DisplayQuestion = choice is AbilityEffect.ChooseCard cardChoice ? AbilityDamageTransferDescription.Question(context, cardChoice) ?? AbilityEffectDescription.Question(
                context.Expressions.World, context.SourceFace, cardChoice)
                : $"{context.Expressions.World.Facts.Title(context.SourceFace)}: choose an option",
            Description = choice is AbilityEffect.ChooseCard describedChoice
                ? AbilityDamageTransferDescription.Summary(context, describedChoice) ?? AbilityEffectDescription.Summary(describedChoice)
                : null,
            ExposesConcealedCandidates = concealedCards,
            PublicKind = concealedCards ? PublicDecisionKind.CardSearch : PublicDecisionKind.Choice,
        };
        return new AbilityStructuralPrompt(prompt, Admission(evidence));
    }
    private static IEnumerable<Affordance> CardChoices(AbilityStructuralContext context,
        AbilityEffect.ChooseCard chooseCard, AbilityContinuationFacts continuation,
        HashSet<AbilityEffect> evidence)
    {
        return LegalCards(context, chooseCard, continuation, evidence)
                .Select(card => new Affordance(
                    card.ObjectId, AbilityStructuralExecution.ChooseVerb, card.ObjectId, card.Owner,
                    EffectiveCards.FaceId(card),
                    Description: DescribeCardChoice(context, chooseCard, card))
                {
                    DisplayLabel = AbilityPlayerChoiceDescription.Commitment(context, chooseCard, card)
                        ?? EffectiveCards.Title(card, context.Expressions.World.Facts),
                    CommitLabel = AbilityDamageTransferDescription.Commitment(context, chooseCard, card)
                        ?? AbilityPlayerChoiceDescription.Commitment(context, chooseCard, card)
                        ?? AbilityPublicInstructionDescription.From(context, chooseCard.Effect,
                            EffectiveCards.Title(card, context.Expressions.World.Facts))
                        ?? AbilityEffectDescription.Choice(chooseCard.Effect,
                        EffectiveCards.Title(card, context.Expressions.World.Facts)),
                });
    }

    private static IEnumerable<Affordance> OptionChoices(AbilityStructuralContext context,
        AbilityEffect.Choose options, AbilityContinuationFacts continuation,
        HashSet<AbilityEffect> evidence)
    {
            bool requiresChange = options.Options.Any(IsExplicitDecline);
            return options.Options
                .Select((option, index) => (Option: option, Index: index))
                .Where(candidate => OptionIsLegal(
                    context, candidate.Option, continuation, requiresChange, evidence))
                .Select(candidate => (candidate.Option, candidate.Index,
                    Meaning: AbilityOptionDescription.From(context, candidate.Option, options)))
                .Select(candidate => new Affordance(
                    candidate.Index, AbilityStructuralExecution.ChooseVerb,
                    context.Expressions.Source.ObjectId, World.Scenario,
                    candidate.Option.OperationName(),
                    Description: string.Join(" ", new[]
                        {
                            options.Descriptions.IsDefaultOrEmpty
                                ? null : options.Descriptions[candidate.Index],
                            candidate.Meaning.Description,
                        }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct()))
                {
                    DisplayLabel = candidate.Meaning.Label,
                    CommitLabel = candidate.Meaning.Label,
                });
    }

    private static string DescribeCardChoice(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card card)
    {
        string description = DescribeCard(context, choice, card);
        return context.Expressions.FinalStep ? $"Final step. {description}" : description;
    }

    private static string DescribeCard(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card card)
    {
        var world = context.Expressions.World;
        string title = EffectiveCards.Title(card, world.Facts);
        if (AbilityDamageTransferDescription.Description(context, choice, card) is { } transfer)
            return $"{title} · {transfer}";
        if (AbilityPlayerChoiceDescription.Description(context, choice, card) is { } draw)
            return $"{title} · {draw}";
        if (EffectiveCards.Kind(card, world.Facts) is CardKind.Hero or CardKind.AlterEgo)
            return $"Select {world.Seats[card.Owner].Name} → {title}";

        if (AbilityChoiceDamageDescription.Description(context, choice, card) is { } damage)
            return damage;

        if (AbilityChoiceThreatDescription.Description(context, choice, card) is { } threat)
            return threat;
        return AbilityPublicInstructionDescription.From(context, choice.Effect, title)
            ?? AbilityEffectDescription.Choice(choice.Effect, title) ?? title;
    }

    internal static bool IsExplicitDecline(AbilityEffect option) =>
        option is AbilityEffect.Sequence { Effects.Length: 0 };

    private static bool InspectsConcealedPile(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.InAreas areas => areas.Areas.Any(area => area is
            AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck),
        AbilityCardSelection.WithTrait filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.FaceDown filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Last filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.InObjectIdOrder filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.WithMatchingPlayerArea filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered =>
            InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Discardable filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Ranked ranked => InspectsConcealedPile(ranked.Cards),
        _ => false,
    };

}
