using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>
/// Pure card-program query and projection policy. It has no resolution state,
/// event sink, runtime registry, or reference to the compatibility facade.
/// </summary>
internal static class AbilityRuntimeQueries
{
    internal static T EffectOf<T>(AbilityEffect effect, AbilityAdmissionContext context)
        where T : AbilityEffect => (T)effect;

    internal static AbilityEffect.Conditional ConditionalOf(
        AbilityEffect effect, AbilityAdmissionContext context) =>
        (AbilityEffect.Conditional)effect;

    internal static long Amount(AbilityNumber number, AbilityAdmissionContext context) =>
        context.Evaluator(areas => SingularAreaQueryIsStable(areas, context)).Amount(number);

    internal static IReadOnlyList<Card> Every(
        AbilityCardSelection selector, AbilityAdmissionContext context) =>
        context.Selectors().Every(selector);

    internal static Card? Find(AbilityCardSelection selector, AbilityAdmissionContext context) =>
        context.Selectors(areas => SingularAreaQueryIsStable(areas, context)).Find(selector);

    internal static bool CanRemoveByEffect(
        AbilityCardSelection selector, AbilityAdmissionContext context, Card card) =>
        context.Selectors().CanRemove(selector, card);

    internal static int Resolver(AbilityAdmissionContext context) =>
        AbilityCardQueries.Resolver(context.Query);

    internal static Card ChosenPlayer(AbilityAdmissionContext context) =>
        AbilityCardQueries.ChosenPlayer(context.Query);

    internal static Card? Named(AbilityCardBinding binding, AbilityAdmissionContext context) =>
        AbilityCardQueries.Named(binding, context.Query);

    internal static long EventModifier(AbilityAdmissionContext context, string kind) =>
        AbilityEventModifiers.Amount(context.World, context.Source, kind);

    internal static AbilityNumber DamageAmountOf(AbilityEffect effect, AbilityAdmissionContext context) =>
        effect switch
        {
            AbilityEffect.Damage damage => damage.Amount,
            AbilityEffect.AttackDamage damage => damage.Amount,
            AbilityEffect.IndirectDamage damage => damage.Amount,
            _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
        };

    internal static AbilityCardSelection DamageSelectionOf(AbilityEffect effect, AbilityAdmissionContext context) =>
        effect switch
        {
            AbilityEffect.Damage damage => damage.Cards,
            AbilityEffect.AttackDamage damage => damage.Cards,
            AbilityEffect.IndirectDamage damage => damage.Among,
            _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
        };

    internal static IReadOnlyList<Card> DamageTargets(
        AbilityCardSelection selector, AbilityAdmissionContext context) => Every(selector, context);

    internal static HashSet<DeckType> SearchAreaTypes(
        AbilityEffect effect, AbilityAdmissionContext context) =>
        EffectOf<AbilityEffect.Search>(effect, context).Areas
            .Select(AbilitySelectorEvaluation.AreaType).ToHashSet();

    internal static long ForEachCount(AbilityEffect effect, AbilityAdmissionContext context)
    {
        long count = Amount(((AbilityEffect.ForEach)effect).Count, context);
        if (count < 0) throw new AbilityException("'forEach' needs a non-negative 'count'");
        return count;
    }

    internal static bool CurrentlyZeroForEach(AbilityEffect effect, AbilityAdmissionContext context) =>
        effect is AbilityEffect.ForEach repeated
        && !(context.Expressions.PowerAmount < 0 && ContainsPowerAmount(repeated.Count))
        && !((context.Reachability.PaymentMayMutate || context.Reachability.PriorStepMayMutate)
             && AmountMayChange(repeated.Count))
        && ForEachCount(effect, context) == 0;

    private static bool AmountMayChange(AbilityNumber number) => number switch
    {
        AbilityNumber.Constant or AbilityNumber.PerPlayer
            or AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.PowerAmount } => false,
        AbilityNumber.Sum sum => sum.Operands.Any(AmountMayChange),
        AbilityNumber.Product product => product.Operands.Any(AmountMayChange),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(AmountMayChange),
        _ => true,
    };

    private static bool ContainsPowerAmount(AbilityNumber number) => number switch
    {
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.PowerAmount } => true,
        AbilityNumber.Sum sum => sum.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Product product => product.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Conditional conditional => ContainsPowerAmount(conditional.Then)
            || ContainsPowerAmount(conditional.Else),
        _ => false,
    };

    internal static IEnumerable<AbilityEffect> ReachableMutationBranches(
        AbilityEffect conditional, AbilityAdmissionContext context) =>
        (context.Reachability.PriorStepMayMutate || context.Reachability.PaymentMayMutate
            || context.Reachability.PriorBindingMayChange)
            ? ConditionalBranches((AbilityEffect.Conditional)conditional)
            : ConditionalBranch(conditional,
                context.Evaluator(areas => SingularAreaQueryIsStable(areas, context)).Test(ConditionalOf(conditional, context).Test)
                    ? "then" : "else") is { } active ? [active] : [];

    internal static long ModifiedAbilityDamage(long amount, AbilityAdmissionContext context) =>
        AbilityAmounts.SaturatingSum(amount, [EventModifier(context, "eventDamage"),
            context.Power == BasicPowers.AttackVerb ? EventModifier(context, "attackDamage") : 0]);

    internal static long CounterCount(Card card, string counter) =>
        AbilityExpressionEvaluation.CounterCount(card, counter);

    internal static AbilityProjectionResolution CombinedOutcomes(
        IEnumerable<AbilityProjectionResolution> values)
    {
        var outcomes = values.ToList();
        return outcomes.Count == 0 || outcomes.All(value => value == AbilityProjectionResolution.None)
            ? AbilityProjectionResolution.None
            : outcomes.All(value => value == AbilityProjectionResolution.Full)
                ? AbilityProjectionResolution.Full : AbilityProjectionResolution.Partial;
    }

    internal static AbilityProjectionResolution ResolutionOfAmount(long available, long wanted) =>
        available <= 0 || wanted <= 0 ? AbilityProjectionResolution.None
        : available >= wanted ? AbilityProjectionResolution.Full : AbilityProjectionResolution.Partial;

    internal static AbilityProjectionResolution ResolutionOf(AbilityEffect effect, AbilityAdmissionContext context) =>
        effect.OperationName() switch
        {
            "seq" or "and" => CombinedOutcomes(OrderedEffects(effect).Select(child => ResolutionOf(child, context))),
            "forEach" when ForEachCount(effect, context) == 0 => AbilityProjectionResolution.None,
            "forEach" => ResolutionOf(EffectBody(effect), context),
            _ => throw new RulesNotImplementedException($"'{context.Source.FaceId}' uses '{effect.OperationName()}' before dependent text, whose projected resolution is not implemented"),
        };

    internal static bool SingularAreaQueryIsStable(IReadOnlySet<DeckType> areas, AbilityAdmissionContext context)
    {
        bool priorCanChange = EffectsMayChangeAnyArea(
            context.Reachability.PriorSteps, areas, context);
        bool paymentCanChange = context.Reachability.PaymentCost is { } cost
            && CostMayChangeAnyArea(cost, areas, context);
        if (priorCanChange || paymentCanChange)
        {
            if (context.Reachability.FilteringContinuationOption)
            {
                return false;
            }
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' reaches a singular area query after its "
                + "matching cards may change"
                + (context.Reachability.PriorSteps.Count > 0
                    ? " during prior effects"
                    : " during payment"));
        }
        return true;
    }

    internal static bool MayChangeAnyArea(
        AbilityEffect effect, IReadOnlySet<DeckType> queried, AbilityAdmissionContext context,
        long multiplier = 1) =>
        new AbilityAreaMutationQuery(queried, context, multiplier).MayChange(effect);

    internal static bool EffectsMayChangeAnyArea(
        IReadOnlyList<AbilityEffect> effects, IReadOnlySet<DeckType> queried,
        AbilityAdmissionContext context, long baseMultiplier = 1) =>
        AbilityAreaProjectionQueries.EffectsMayChangeAnyArea(
            effects, queried, context, baseMultiplier);

    internal static bool CostMayChangeAnyArea(
        AbilityCost cost, IReadOnlySet<DeckType> queried, AbilityAdmissionContext context) =>
        AbilityAreaProjectionQueries.CostMayChangeAnyArea(cost, queried, context);

}
