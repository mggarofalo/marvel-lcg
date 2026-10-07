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
                    Description: DescribeCard(context, chooseCard, card))
                {
                    DisplayLabel = AbilityPlayerChoiceDescription.Commitment(context, chooseCard, card)
                        ?? EffectiveCards.Title(card, context.Expressions.World.Facts),
                    CommitLabel = AbilityDamageTransferDescription.Commitment(context, chooseCard, card)
                        ?? AbilityPlayerChoiceDescription.Commitment(context, chooseCard, card)
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
                    Meaning: AbilityOptionDescription.From(context, candidate.Option)))
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

        if (ProjectedDamage(context, choice.Effect) is { } projection)
            return DescribeDamage(context, projection, card, title);

        if (choice.Effect is AbilityEffect.RemoveThreat threat)
        {
            long current = card.Tokens.GetValueOrDefault("k_threat");
            long result = current - Math.Min(current, Amount(threat.Amount, context.Expressions));
            long threshold = world.Facts.PrintedValue(
                card.FaceId, "TargetThreat", world.Players);
            return threshold > 0
                ? $"{title} · {current}/{threshold} → {result}/{threshold} threat"
                : $"{title} · {current} → {result} threat";
        }
        return AbilityEffectDescription.Choice(choice.Effect, title) ?? title;
    }

    private static string DescribeDamage(AbilityStructuralContext context,
        (AbilityNumber Amount, bool IsAttack, bool Overkill) projection, Card card, string title)
    {
        var world = context.Expressions.World;
            Card attacker = context.AbilityActor
                ?? world.Seats[AbilityCardQueries.Resolver(
                    context.Expressions.Bindings)].IdentityCard;
            if (projection.IsAttack
                && Statuses.Afflicted(world, world.Facts, attacker, Statuses.Stunned))
            {
                return $"{title} · Stunned cancels this attack; no damage will be dealt";
            }

            long amount = ProjectedDamageAmount(context, projection.Amount, projection.IsAttack);
            string consequence = projection.IsAttack
                ? Damage.PreviewAttack(
                    world, world.Facts, attacker, context.Expressions.Source, card,
                    amount, projection.Overkill)
                : Damage.PreviewDamage(
                    world, world.Facts, context.Expressions.Source, card, amount);
            return $"{title} · {consequence}";
    }

    private static (AbilityNumber Amount, bool IsAttack, bool Overkill)? ProjectedDamage(
        AbilityStructuralContext context, AbilityEffect? effect, bool attack = false)
    {
        if (effect is AbilityEffect.Power { Kind: AbilityPowerKind.Attack } power)
            return ProjectedDamage(context, power.Effect, attack: true);
        if (effect is AbilityEffect.Conditional conditional)
            return ProjectedDamage(context,
                Test(conditional.Test, context.Expressions)
                    ? conditional.Then : conditional.Else, attack);
        if (effect is AbilityEffect.Sequence sequence)
            return ProjectedDamage(context, sequence.Effects.FirstOrDefault(), attack);
        return effect switch
        {
            AbilityEffect.AttackDamage damage => (damage.Amount, true, damage.Overkill),
            AbilityEffect.Damage damage => (damage.Amount, attack, false),
            _ => null,
        };
    }

    private static long ProjectedDamageAmount(
        AbilityStructuralContext context, AbilityNumber damage, bool attack)
    {
        var world = context.Expressions.World;
        long amount = AbilityAmounts.SaturatingSum(
            Amount(damage, context.Expressions),
            [AbilityEventModifiers.Amount(world, context.Expressions.Source, "eventDamage")]);
        return attack
            ? AbilityAmounts.SaturatingSum(amount,
                [AbilityEventModifiers.Amount(world, context.Expressions.Source, "attackDamage")])
            : amount;
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
