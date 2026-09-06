using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Typed facts reconstructed at the legacy continuation boundary. These carry
// authored nodes and cursor values, not the string wire that encoded them.
internal abstract record AbilityContinuationFrame;
internal sealed record SequenceContinuationFrame(
    AbilityEffect.Sequence Parent, int Current) : AbilityContinuationFrame;
internal sealed record DependentContinuationFrame(
    AbilityEffect.Dependent Parent, bool Predecessor,
    AbilityStructuralOutcome? Outcome) : AbilityContinuationFrame;
internal sealed record SimultaneousContinuationFrame(
    AbilityEffect.Simultaneous Parent, ImmutableArray<int> Remaining)
    : AbilityContinuationFrame;
internal sealed record ForEachContinuationFrame(
    AbilityEffect.ForEach Parent, long Current, long Count)
    : AbilityContinuationFrame;
internal sealed record EachTimeContinuationFrame(
    AbilityEffect.EachTime Parent, long Current, long Count)
    : AbilityContinuationFrame;
internal sealed record EachPlayerContinuationFrame(bool StopsOuterContinuation)
    : AbilityContinuationFrame;

internal sealed record AbilityContinuationFacts(
    bool HasPath, ImmutableArray<AbilityContinuationFrame> Frames)
{
    internal static AbilityContinuationFacts Empty { get; } = new(false, []);
}

internal sealed record AbilityStructuralPrompt(
    Prompt Prompt, AbilityAdmissionResult Admission);

/// <summary>Choice legality over concrete immutable expression and suffix facts.</summary>
internal static class AbilityStructuralQueries
{
    internal static AbilityStructuralPrompt DescribeChoice(
        AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation)
    {
        var evidence = NewEvidence();
        bool cards = choice is AbilityEffect.ChooseCard;
        IEnumerable<Affordance> affordances;
        if (choice is AbilityEffect.ChooseCard chooseCard)
        {
            affordances = LegalCards(context, chooseCard, continuation, evidence)
                .Select(card => new Affordance(
                    card.ObjectId, AbilityStructuralExecution.ChooseVerb, card.ObjectId, card.Owner,
                    card.FaceId, Description: DescribeCard(context, chooseCard, card)));
        }
        else if (choice is AbilityEffect.Choose options)
        {
            bool requiresChange = options.Options.Any(IsExplicitDecline);
            affordances = options.Options
                .Select((option, index) => (Option: option, Index: index))
                .Where(candidate => OptionIsLegal(
                    context, candidate.Option, continuation, requiresChange, evidence))
                .Select(candidate => new Affordance(
                    candidate.Index, AbilityStructuralExecution.ChooseVerb,
                    context.Expressions.Source.ObjectId, World.Scenario,
                    candidate.Option.OperationName(),
                    Description: options.Descriptions.IsDefaultOrEmpty
                        ? null : options.Descriptions[candidate.Index]));
        }
        else
        {
            throw new InvalidOperationException(
                $"'{context.SourceFace}' does not contain a generic choice");
        }

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
            Cancellable: false, offered)
        {
            ExposesConcealedCandidates = choice is AbilityEffect.ChooseCard cardChoice
                && InspectsConcealedPile(cardChoice.From),
        };
        return new AbilityStructuralPrompt(prompt, Admission(evidence));
    }

    internal static AbilityStructuralTransition AnswerChoice(
        AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation, Decision answer)
    {
        var evidence = NewEvidence();
        if (choice is AbilityEffect.ChooseCard chooseCard)
        {
            var selected = LegalCards(context, chooseCard, continuation, evidence)
                .FirstOrDefault(card => card.ObjectId == answer.Affordance);
            if (selected is null)
                return new Unsupported(
                    $"'{context.SourceFace}' did not offer card {answer.Affordance} to choose");

            var selectedContext = WithSelection(context, selected);
            AbilityStructuralOutcome? pending = null;
            if (context.HasPendingDependency
                && !AbilityInitiation.ActiveChoices(
                    chooseCard.Effect, selectedContext.Admission()).Any())
            {
                pending = Outcome(AbilityInitiation.ResolutionOf(
                    chooseCard.Effect, selectedContext.Admission()));
            }
            return new RunChoice(
                chooseCard.Effect, new ChoiceFrame(null, selected.ObjectId), selected,
                BindsPlayerSelection: true, pending, Admission(evidence));
        }

        var options = (AbilityEffect.Choose)choice;
        if (answer.IsDecline || answer.Affordance < 0
            || answer.Affordance >= options.Options.Length)
        {
            return new Unsupported(
                $"'{context.SourceFace}' offers {options.Options.Length} options and none "
                + $"of them is number {answer.Affordance}");
        }

        var selectedOption = options.Options[answer.Affordance];
        bool requiresChange = options.Options.Any(IsExplicitDecline);
        if (!OptionIsLegal(
            context, selectedOption, continuation, requiresChange, evidence))
        {
            return new Unsupported(
                $"'{context.SourceFace}' cannot choose illegal option {answer.Affordance}");
        }

        AbilityStructuralOutcome? optionOutcome = context.HasPendingDependency
            ? Outcome(AbilityInitiation.ResolutionOf(
                selectedOption, context.Admission()))
            : null;
        return new RunChoice(
            selectedOption, new ChoiceFrame(answer.Affordance, null), null,
            BindsPlayerSelection: false, optionOutcome, Admission(evidence));
    }

    /// <summary>Whether one listed option may be chosen and leaves its suffix resolvable.</summary>
    /// <remarks>
    /// <c>rr:choose-option.1</c> requires an encounter-card option's targets to be
    /// valid. <c>rr:choose-option.2</c> requires a player-card option to change the
    /// game at least partially; an empty sequence is the explicit decline branch.
    /// </remarks>
    private static bool OptionIsLegal(
        AbilityStructuralContext context, AbilityEffect option,
        AbilityContinuationFacts continuation, bool requireStateChange,
        HashSet<AbilityEffect> evidence)
    {
        var admission = context.Admission();
        bool local = AbilityInitiation.IsOptionLegal(option, admission)
            && (!requireStateChange || IsExplicitDecline(option)
                || AbilityInitiation.CanPartiallyResolve(option, admission));
        if (!local || !continuation.HasPath)
            return local;

        var prior = admission.Query.ChosenBinding;
        AbilityInitiation.ResolutionOutcome? pending = context.HasPendingDependency
            ? AbilityInitiation.ResolutionOf(option, admission)
            : null;
        var outcomes = AbilityInitiation.BindingCandidates(
            option, admission,
            new AbilityInitiation.BindingCandidateState(
                prior is null ? [] : [prior.Card], prior is null));
        var after = admission.WithReachability(admission.Reachability with
        {
            PriorSteps = admission.Reachability.PriorSteps.Add(option),
            FilteringContinuationOption = true,
        });
        return ContinuationCanResolve(
            context, continuation, outcomes, after, pending, evidence);
    }

    /// <summary>Targets meeting both the selector, nested effect, and remaining suffix.</summary>
    /// <remarks>
    /// <c>rr:target.2.2</c> makes choose-card a target selection, so each candidate
    /// is bound before the nested effect and structural continuation are admitted.
    /// </remarks>
    private static List<Card> LegalCards(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice,
        AbilityContinuationFacts continuation, HashSet<AbilityEffect> evidence)
    {
        var legal = AbilityInitiation.LegalCardChoices(choice, context.Admission());
        if (!continuation.HasPath)
            return legal;

        return legal.Where(candidate =>
        {
            var selected = context.Admission().WithSelection(candidate);
            AbilityInitiation.ResolutionOutcome? pending = context.HasPendingDependency
                && !AbilityInitiation.ActiveChoices(choice.Effect, selected).Any()
                    ? AbilityInitiation.ResolutionOf(choice.Effect, selected)
                    : null;
            var outcomes = AbilityInitiation.BindingCandidates(
                choice.Effect, selected,
                new AbilityInitiation.BindingCandidateState([candidate], false));
            var after = selected.WithReachability(selected.Reachability with
            {
                PriorSteps = selected.Reachability.PriorSteps.Add(choice.Effect),
                FilteringContinuationOption = true,
            });
            return ContinuationCanResolve(
                context, continuation, outcomes, after, pending, evidence);
        }).ToList();
    }

    private static bool ContinuationCanResolve(
        AbilityStructuralContext context, AbilityContinuationFacts continuation,
        AbilityInitiation.BindingCandidateState outcomes,
        AbilityAdmissionContext admission,
        AbilityInitiation.ResolutionOutcome? pending,
        HashSet<AbilityEffect> evidence)
    {
        bool CanResolve(Card? binding)
        {
            var selected = admission.WithReachability(admission.Reachability with
            {
                CheckingInitiation = true,
                PriorBindingCandidates = binding is null ? [] : [binding],
                PriorBindingMayBeEmpty = binding is null,
                PriorBindingMayChange = false,
            }).WithSelection(binding);
            var remaining = Remaining(context, continuation, selected, pending, evidence);
            if (remaining.Count == 0)
                return true;

            var sensitive = new HashSet<DeckType>();
            foreach (var step in remaining)
                AbilityAdmissionAreaDependencies.Collect(step, selected, sensitive);
            if (sensitive.Count > 0
                && AbilityRuntimeQueries.EffectsMayChangeAnyArea(
                    selected.Reachability.PriorSteps, sensitive, selected))
            {
                return false;
            }

            var sequence = new AbilityEffect.Sequence([.. remaining]);
            var admitted = AbilityInitiation.AdmitStructure(sequence, selected);
            AddEvidence(evidence, admitted);
            return admitted.IsAdmissible
                && AbilityInitiation.TargetsAreValid(sequence, selected);
        }

        return outcomes.Cards.Any(CanResolve)
            || outcomes.MayBeEmpty && CanResolve(null);
    }

    private static List<AbilityEffect> Remaining(
        AbilityStructuralContext context, AbilityContinuationFacts continuation,
        AbilityAdmissionContext admission,
        AbilityInitiation.ResolutionOutcome? pending,
        HashSet<AbilityEffect> evidence)
    {
        var remaining = new List<AbilityEffect>();
        for (int position = continuation.Frames.Length - 1; position >= 0; position--)
        {
            switch (continuation.Frames[position])
            {
                case EachPlayerContinuationFrame { StopsOuterContinuation: true }:
                    return remaining;
                case SequenceContinuationFrame sequence:
                    remaining.AddRange(sequence.Parent.Effects.Skip(sequence.Current + 1));
                    break;
                case DependentContinuationFrame dependent
                    when dependent.Predecessor:
                    AbilityStructuralOutcome? outcome = dependent.Outcome
                        ?? (pending is { } recorded ? Outcome(recorded) : null);
                    var required = dependent.Parent.OnFull
                        ? AbilityStructuralOutcome.Full
                        : AbilityStructuralOutcome.None;
                    if (outcome == required)
                        remaining.Add(dependent.Parent.Continuation);
                    break;
                case SimultaneousContinuationFrame simultaneous:
                    remaining.AddRange(simultaneous.Remaining.Select(
                        index => simultaneous.Parent.Effects[index]));
                    break;
                case ForEachContinuationFrame repeated:
                    for (long next = repeated.Current + 1; next < repeated.Count; next++)
                        remaining.Add(repeated.Parent.Effect);
                    break;
                case EachTimeContinuationFrame repeated
                    when repeated.Current + 1 < repeated.Count
                        && LaterEachTimePromptIsGuaranteed(
                            context, repeated, admission, evidence):
                    return remaining;
            }
        }
        return remaining;
    }

    private static bool LaterEachTimePromptIsGuaranteed(
        AbilityStructuralContext context, EachTimeContinuationFrame repeated,
        AbilityAdmissionContext admission, HashSet<AbilityEffect> evidence)
    {
        long count = repeated.Count - repeated.Current - 1;
        var future = context.Expressions.World.AreaOf(DeckType.EncounterDeck).Cards
            .Reverse().Take((int)Math.Min(count, int.MaxValue)).ToList();
        if (future.Count < count)
            return false;

        foreach (var card in future)
        {
            var altered = admission.WithAltered(card);
            if (Test(repeated.Parent.When, altered.Expressions)
                && AbilityInitiation.ActiveChoices(repeated.Parent.Then, altered).Any())
            {
                var admitted = AbilityInitiation.Admit(repeated.Parent.Then, altered);
                AddEvidence(evidence, admitted);
                if (admitted.IsAdmissible)
                    return true;
            }
        }
        return false;
    }

    private static string DescribeCard(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card card)
    {
        var world = context.Expressions.World;
        string title = world.Facts.Title(card.FaceId);
        if (world.Facts.Kind(card.FaceId) is CardKind.Hero or CardKind.AlterEgo)
            return $"Select {world.Seats[card.Owner].Name} → {title}";

        if (ProjectedDamage(context, choice.Effect) is { } projection)
        {
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
        return title;
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

    private static AbilityStructuralContext WithSelection(
        AbilityStructuralContext context, Card card)
    {
        var admission = context.Admission().WithSelection(card);
        return context with { Expressions = admission.Expressions };
    }

    private static long Amount(AbilityNumber number, AbilityExpressionContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Amount(number)), context.World);
    }

    private static bool Test(AbilityCondition condition, AbilityExpressionContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Test(condition)), context.World);
    }

    private static AbilityExpressionEvaluation Evaluation(AbilityExpressionContext context) =>
        new(context, new AbilitySelectorEvaluation(context.Bindings));

    private static bool IsExplicitDecline(AbilityEffect option) =>
        option is AbilityEffect.Sequence { Effects.Length: 0 };

    private static bool InspectsConcealedPile(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.InAreas areas => areas.Areas.Any(area => area is
            AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck),
        AbilityCardSelection.WithTrait filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered =>
            InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Discardable filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Ranked ranked => InspectsConcealedPile(ranked.Cards),
        _ => false,
    };

    private static HashSet<AbilityEffect> NewEvidence() =>
        new(ReferenceEqualityComparer.Instance);

    private static void AddEvidence(
        HashSet<AbilityEffect> evidence, AbilityAdmissionResult admitted) =>
        evidence.UnionWith(admitted.CrisisIgnoringThwarts);

    private static AbilityAdmissionResult Admission(HashSet<AbilityEffect> evidence) =>
        new(true, ImmutableHashSet.CreateRange<AbilityEffect>(
            ReferenceEqualityComparer.Instance, evidence));

    private static AbilityStructuralOutcome Outcome(
        AbilityInitiation.ResolutionOutcome outcome) =>
        (AbilityStructuralOutcome)(int)outcome;

    private static T Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information)
            world.RecordInformation(observation);
        return result.Value;
    }
}
