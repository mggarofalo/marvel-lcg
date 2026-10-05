using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Queries card selection and resolution against a projected board.</summary>
internal static class AbilityProjectedStateQueries
{
    internal static List<Card> ProjectedEvery(
        AbilityCardSelection selector, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        var found = Every(selector, context)
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .ToList();
        return selector switch
        {
            AbilityCardSelection.Bound bound =>
                ProjectedBound(bound, found, state),
            AbilityCardSelection.EnemiesWithTrait enemies =>
                ProjectedEnemiesWithTrait(enemies, state, context),
            AbilityCardSelection.Ranked ranked =>
                ProjectedRanked(ranked, state, context),
            AbilityCardSelection.Last last => [.. ProjectedEvery(last.Cards, state, context).TakeLast(1)],
            AbilityCardSelection.FaceDown filtered => [.. ProjectedEvery(filtered.Cards, state, context).Where(card => !card.FaceUp)],
            AbilityCardSelection.InObjectIdOrder ordered => [.. ProjectedEvery(ordered.Cards, state, context).OrderBy(card => card.ObjectId)],
            AbilityCardSelection.WithTrait trait =>
                ProjectedWithTrait(trait, state, context),
            AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain } =>
                ProjectedVillain(found, state, context),
            AbilityCardSelection.Query query =>
                ProjectedQuery(query, found, state, context),
            AbilityCardSelection.Titled titled =>
                ProjectedTitled(titled, found, state, context),
            _ => Distinct(found),
        };
    }

    private static List<Card> ProjectedBound(
        AbilityCardSelection.Bound bound, List<Card> found,
        AreaProjectionState state) =>
        bound.Binding == AbilityCardBinding.This && !state.SourceReferenceCurrent
            ? [] : found;

    private static List<Card> ProjectedEnemiesWithTrait(
        AbilityCardSelection.EnemiesWithTrait enemies, AreaProjectionState state,
        AbilityAdmissionContext context) =>
        [.. ProjectedEvery(
                new AbilityCardSelection.Query(AbilityCardQuery.Enemies), state, context)
            .Where(card => state.HasTrait(context, card, enemies.Trait))];

    private static List<Card> ProjectedRanked(
        AbilityCardSelection.Ranked ranked, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        var among = ProjectedEvery(ranked.Cards, state, context)
            .Where(card => state.Entered.Contains(card.ObjectId)
                ? Rules.Play.Discard.EffectCanRemove(
                    context.World, context.World.Facts, context.Source, card)
                : CanRemoveByEffect(ranked.Cards, context, card))
            .ToList();
        if (among.Count == 0)
        {
            return [];
        }
        long Rank(Card card) => ProjectedRank(ranked.By, card, state, context);
        long extreme = ranked.Maximum ? among.Max(Rank) : among.Min(Rank);
        return [.. among.Where(card => Rank(card) == extreme)];
    }

    private static long ProjectedRank(
        AbilityCardRank rank, Card card, AreaProjectionState state,
        AbilityAdmissionContext context) => rank switch
        {
            AbilityCardRank.Cost => context.World.Facts.PrintedValue(
                card.FaceId, "Cost", context.World.Players),
            AbilityCardRank.Attack => state.ModifiedOf(context, card, "attack"),
            AbilityCardRank.PrintedHealth => EffectiveCards.BaseValue(
                card, context.World.Facts, "HP", context.World.Players),
            _ => throw new InvalidOperationException(
                "Unknown compiled card rank in area projection"),
        };

    private static List<Card> ProjectedWithTrait(
        AbilityCardSelection.WithTrait trait, AreaProjectionState state,
        AbilityAdmissionContext context) =>
        [.. ProjectedEvery(trait.Cards, state, context)
            .Where(card => state.HasTrait(context, card, trait.Trait))];

    private static List<Card> ProjectedVillain(
        List<Card> found, AreaProjectionState state, AbilityAdmissionContext context)
    {
        if (state.ActiveVillain < 0)
        {
            return Distinct(found);
        }
        found.RemoveAll(card =>
            CardKinds.IsVillain(EffectiveCards.Kind(card, context.World.Facts)));
        found.Add(context.World.Cards[state.ActiveVillain]);
        return Distinct(found);
    }

    private static List<Card> ProjectedQuery(
        AbilityCardSelection.Query query, List<Card> found,
        AreaProjectionState state, AbilityAdmissionContext context)
    {
        if (query.Kind is AbilityCardQuery.AttackableEnemies
            or AbilityCardQuery.AttackableMinions)
        {
            return ProjectedAttackable(query.Kind, state, context);
        }
        found.AddRange(state.Entered
            .Select(id => context.World.Cards[id])
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .Where(card => EnteredMatchesQuery(query.Kind, card, state, context)));
        return Distinct(found);
    }

    private static List<Card> ProjectedAttackable(
        AbilityCardQuery query, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        var candidates = query == AbilityCardQuery.AttackableEnemies
            ? AbilityCardQuery.Enemies : AbilityCardQuery.Minions;
        var found = ProjectedEvery(
                new AbilityCardSelection.Query(candidates), state, context)
            .Where(card => AbilityProgramQueries.CanTakeDamage(
                context.World, context.Program, card, context.Source))
            .Where(card => query != AbilityCardQuery.AttackableMinions
                || EffectiveCards.Kind(card, context.World.Facts) == CardKind.Minion)
            .ToList();
        if (found.Any(card => IsGuard(card, state, context)))
        {
            found.RemoveAll(card =>
                CardKinds.IsVillain(EffectiveCards.Kind(card, context.World.Facts)));
        }
        return found;
    }

    private static bool IsGuard(
        Card card, AreaProjectionState state, AbilityAdmissionContext context) =>
        EffectiveCards.Kind(card, context.World.Facts) == CardKind.Minion
        && state.ModifiedOf(context, card, "guard") > 0
        && (state.EngagedWith.GetValueOrDefault(card.ObjectId, -1) == Resolver(context)
            || !state.EngagedWith.ContainsKey(card.ObjectId)
                && card.Area.PlayArea == PlayArea.Of(Resolver(context)));

    private static bool EnteredMatchesQuery(
        AbilityCardQuery query, Card card, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        var kind = EffectiveCards.Kind(card, context.World.Facts);
        return query switch
        {
            AbilityCardQuery.Minions => kind == CardKind.Minion,
            AbilityCardQuery.MinionsEngagedWithYou => kind == CardKind.Minion
                && state.EngagedWith.GetValueOrDefault(card.ObjectId, -1)
                    == Resolver(context),
            AbilityCardQuery.EnemiesEngagedWithChosenPlayer =>
                kind == CardKind.Minion
                && state.EngagedWith.GetValueOrDefault(card.ObjectId, -1)
                    == ChosenPlayer(context).Owner,
            AbilityCardQuery.Enemies => CardKinds.IsEnemy(kind),
            AbilityCardQuery.Characters => CardKinds.IsCharacter(kind),
            _ => IsSchemeQuery(query) && kind == CardKind.EncounterSideScheme,
        };
    }

    private static bool IsSchemeQuery(AbilityCardQuery query) =>
        query is AbilityCardQuery.SideSchemes or AbilityCardQuery.Schemes
            or AbilityCardQuery.ThwartableSchemes;

    private static List<Card> ProjectedTitled(
        AbilityCardSelection.Titled titled, List<Card> found,
        AreaProjectionState state, AbilityAdmissionContext context)
    {
        found.AddRange(state.Entered
            .Select(id => context.World.Cards[id])
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .Where(card => string.Equals(
                context.World.Facts.Title(card.FaceId), titled.Title,
                StringComparison.Ordinal)));
        return Distinct(found);
    }

    private static List<Card> Distinct(IEnumerable<Card> cards) =>
        [.. cards.DistinctBy(card => card.ObjectId)];

    internal static Card? ProjectedFind(
        AbilityCardSelection selector, AreaProjectionState state, AbilityAdmissionContext context)
    {
        var found = ProjectedEvery(selector, state, context);
        return found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' projects {found.Count} cards where one is required"),
        };
    }

    internal static bool? ProjectedTest(
        AbilityCondition test, AreaProjectionState state, AbilityAdmissionContext context)
    {
        if (test is AbilityCondition.CardText
            { Property: AbilityCardTextProperty.Status } status)
        {
            return ProjectedStatusTest(status, state, context);
        }
        if (test is AbilityCondition.Negated negated)
        {
            return ProjectedTest(negated.Operand, state, context) is { } inner
                ? !inner : null;
        }
        return test is AbilityCondition.All or AbilityCondition.Any
            ? ProjectedCompoundTest(test, state, context)
            : null;
    }

    private static bool? ProjectedStatusTest(
        AbilityCondition.CardText status, AreaProjectionState state,
        AbilityAdmissionContext context) =>
        ProjectedFind(status.Card, state, context) is { } target
            ? state.StatusOf(context, target, status.Text) > 0
            : null;

    private static bool? ProjectedCompoundTest(
        AbilityCondition test, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        var operands = test is AbilityCondition.All all
            ? all.Operands : ((AbilityCondition.Any)test).Operands;
        var values = operands
            .Select(child => ProjectedTest(child, state, context)).ToList();
        if (test is AbilityCondition.All && values.Any(value => value == false))
        {
            return false;
        }
        if (test is AbilityCondition.Any && values.Any(value => value == true))
        {
            return true;
        }
        return values.Any(value => value is null)
            ? null
            : test is AbilityCondition.All;
    }

    internal static AbilityProjectionResolution ProjectedResolution(
        AbilityEffect effect, AreaProjectionState state, AbilityAdmissionContext context) =>
        effect.OperationName() switch
        {
            "seq" or "and" => CombinedOutcomes(
                OrderedEffects(effect).Select(child =>
                    ProjectedResolution(child, state, context))),
            "if" => ProjectedTest(
                    ConditionalOf(effect, context).Test, state, context)
                is { } result
                    ? ConditionalBranch(effect, result ? "then" : "else") is { } branch
                        ? ProjectedResolution(branch, state, context)
                        : AbilityProjectionResolution.None
                    : throw new RulesNotImplementedException(
                        $"'{context.Source.FaceId}' has a projected dependent condition "
                        + "whose outcome is not implemented"),
            "forEach" when ForEachCount(effect, context) == 0 =>
                AbilityProjectionResolution.None,
            "forEach" => ProjectedResolution(
                EffectBody(effect), state, context),
            "heal" => ResolutionOfAmount(
                ProjectedFind(EffectOf<AbilityEffect.Heal>(effect, context).Card, state, context) is { } healed
                    ? state.DamageOf(healed) : 0,
                Amount(EffectOf<AbilityEffect.Heal>(effect, context).Amount, context)),
            "removeThreat" => CombinedOutcomes(
                ProjectedEvery(EffectOf<AbilityEffect.RemoveThreat>(effect, context).Schemes, state, context).Select(scheme =>
                    ResolutionOfAmount(
                        state.ThreatOf(scheme),
                        Amount(EffectOf<AbilityEffect.RemoveThreat>(effect, context).Amount, context)))),
            _ => ResolutionOf(effect, context),
        };

}
