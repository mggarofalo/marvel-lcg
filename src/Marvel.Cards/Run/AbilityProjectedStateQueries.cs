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
        AbilityInPlayCardQuery.ValidateProjection(selector, state);
        if (selector is AbilityCardSelection.Query relation
            && AbilityProjectedRelationQueries.Select(relation.Kind, state, context) is { } related)
            return related;
        var found = ProjectedCandidates(selector, state, context)
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .ToList();
        return selector switch
        {
            AbilityCardSelection.Bound bound =>
                ProjectedBound(bound, found, state),
            AbilityCardSelection.EnemiesWithTrait enemies =>
                ProjectedEnemiesWithTrait(enemies, state, context),
            AbilityCardSelection.Ranked ranked =>
                AbilityAreaRankedSelection.Select(ranked, state, context),
            AbilityCardSelection.FirstAvailable fallback => fallback.Options.Select(option => ProjectedEvery(option, state, context)).FirstOrDefault(cards => cards.Count > 0) ?? [],
            AbilityCardSelection.Last last => [.. ProjectedEvery(last.Cards, state, context).TakeLast(1)],
            AbilityCardSelection.FaceDown filtered => [.. ProjectedEvery(filtered.Cards, state, context).Where(card => !card.FaceUp)],
            AbilityCardSelection.InObjectIdOrder ordered => [.. ProjectedEvery(ordered.Cards, state, context).OrderBy(card => card.ObjectId)],
            AbilityCardSelection.WithTrait trait =>
                ProjectedWithTrait(trait, state, context),
            AbilityCardSelection.WithStatus status =>
                [.. ProjectedEvery(status.Cards, state, context)
                    .Where(card => (DeckTypes.IsInPlay(card.Area.Type)
                            || state.Entered.Contains(card.ObjectId))
                        && !state.Departed.Contains(card.ObjectId)
                        && ProjectedAfflicted(status.Status, card, state, context))],
            AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain } =>
                ProjectedVillain(found, state, context),
            AbilityCardSelection.Query query =>
                ProjectedQuery(query, found, state, context),
            AbilityCardSelection.Titled titled =>
                ProjectedTitled(titled, found, state, context),
            _ => Distinct(found),
        };
    }

    private static IEnumerable<Card> ProjectedCandidates(
        AbilityCardSelection selector, AreaProjectionState state, AbilityAdmissionContext context)
    {
        var candidates = Every(selector, context);
        if (context.World.EncounterTopology is null
            || selector is not AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen })
            return candidates;
        // rr:villain-defeat.3.1: same-title villain stages are "the same character".
        return candidates.Select(card => (Original: card,
                Current: AbilityAreaEncounterProjection.Current(context.World, state, card)))
            .Where(pair => pair.Current is not null && string.Equals(
                context.World.Facts.Title(pair.Original.FaceId), context.World.Facts.Title(pair.Current.FaceId),
                StringComparison.Ordinal))
            .Select(pair => pair.Current!);
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

    private static List<Card> ProjectedWithTrait(
        AbilityCardSelection.WithTrait trait, AreaProjectionState state,
        AbilityAdmissionContext context) =>
        [.. ProjectedEvery(trait.Cards, state, context)
            .Where(card => state.HasTrait(context, card, trait.Trait) != trait.Exclude)];

    private static bool ProjectedAfflicted(string status, Card card,
        AreaProjectionState state, AbilityAdmissionContext context)
    {
        if (status is Statuses.Stunned or Statuses.Confused
            && state.ModifiedOf(context, card, "stalwart") > 0) return false;
        long needed = status is Statuses.Stunned or Statuses.Confused
            && state.ModifiedOf(context, card, "steady") > 0 ? 2 : 1;
        return state.StatusOf(context, card, status) >= needed;
    }

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
        var restricting = ProjectedEvery(new AbilityCardSelection.Query(AbilityCardQuery.Villains), state, context)
            .Where(card => state.ModifiedOf(context, card, VillainAttackRestrictions.OtherVillains) > 0)
            .ToList();
        found.RemoveAll(card => CardKinds.IsVillain(EffectiveCards.Kind(card, context.World.Facts))
            && !VillainAttackRestrictions.Allows(card, restricting));
        // The projection keeps the live attack-target filter; no admitted action
        // is offered or withheld on this difference alone.
        found.RemoveAll(card => !AbilityAttackTargets.Permits(context.World, Resolver(context), context.Source, card));
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
            AbilityCardQuery.Villains => CardKinds.IsVillain(kind),
            AbilityCardQuery.Enemies => CardKinds.IsEnemy(kind),
            AbilityCardQuery.Characters => CardKinds.IsCharacter(kind),
            AbilityCardQuery.Allies => kind == CardKind.Ally,
            AbilityCardQuery.Supports => kind == CardKind.Support,
            AbilityCardQuery.HeroesAndAllies => kind is CardKind.Hero or CardKind.AlterEgo or CardKind.Ally,
            AbilityCardQuery.HeroAndAllyTargets => kind is CardKind.Hero or CardKind.Ally,
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

    internal static AbilityProjectionResolution ProjectedResolution(
        AbilityEffect effect, AreaProjectionState state, AbilityAdmissionContext context) =>
        effect.OperationName() switch
        {
            "changeForm" => AbilityAreaFormProjection.Destination(
                    (AbilityEffect.ChangeForm)effect, state, context) is null
                ? AbilityProjectionResolution.None : AbilityProjectionResolution.Full,
            "moveActiveCounter" => AbilityActiveCounterProjection.CanMoveArea(effect, state, context)
                ? AbilityProjectionResolution.Full : AbilityProjectionResolution.None,
            "seq" or "and" => CombinedOutcomes(
                OrderedEffects(effect).Select(child =>
                    ProjectedResolution(child, state, context))),
            "if" => AbilityProjectedConditions.Test(
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
            "setHitPoints" => AbilityHitPointAssignment.ProjectedOutcome(effect, state, context),
            "removeThreat" => CombinedOutcomes(
                ProjectedEvery(EffectOf<AbilityEffect.RemoveThreat>(effect, context).Schemes, state, context).Select(scheme =>
                    ResolutionOfAmount(
                        state.ThreatOf(scheme),
                        AbilityEventModifiers.ThreatRemovalAmount(EffectOf<AbilityEffect.RemoveThreat>(effect, context), context.World, context.Source, Amount(EffectOf<AbilityEffect.RemoveThreat>(effect, context).Amount, context))))),
            _ => ResolutionOf(effect, context),
        };

}
