using static Marvel.Cards.Run.AbilityExpressionAreaDependencies;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityAdmissionAreaDependencies
{
    private static readonly ImmutableArray<Type> OrderedContainerTypes =
    [
        typeof(AbilityEffect.Sequence), typeof(AbilityEffect.Simultaneous),
        typeof(AbilityEffect.Choose),
    ];

    private static readonly ImmutableArray<Type> BranchingContainerTypes =
    [
        typeof(AbilityEffect.Conditional), typeof(AbilityEffect.Dependent),
        typeof(AbilityEffect.EachTime),
    ];

    private static readonly ImmutableArray<Type> WrappedContainerTypes =
    [
        typeof(AbilityEffect.EachPlayer), typeof(AbilityEffect.ForEach),
        typeof(AbilityEffect.ChooseCard), typeof(AbilityEffect.AfterActivation),
        typeof(AbilityEffect.Power), typeof(AbilityEffect.ThwartGroup),
        typeof(AbilityEffect.PayOrEffect),
    ];

    private static readonly ImmutableArray<Type> SelectionDependencyTypes =
    [
        typeof(AbilityEffect.CardAction), typeof(AbilityEffect.Heal), typeof(AbilityEffect.SetHitPoints),
        typeof(AbilityEffect.MoveThreat), typeof(AbilityEffect.MoveDamage), typeof(AbilityEffect.RemoveThreat),
        typeof(AbilityEffect.GrantTrait), typeof(AbilityEffect.GrantField),
        typeof(AbilityEffect.PreventDamageWhile),
    ];

    private static readonly ImmutableArray<Type> CardMutationDependencyTypes =
    [
        typeof(AbilityEffect.DelayedDiscard), typeof(AbilityEffect.DealEncounterCard),
        typeof(AbilityEffect.PlaceAtRandom), typeof(AbilityEffect.Search),
        typeof(AbilityEffect.PutIntoPlay), typeof(AbilityEffect.PlaceCounters),
        typeof(AbilityEffect.RemoveCounters), typeof(AbilityEffect.StoreBeneath), typeof(AbilityEffect.AttachCards),
    ];

    private static readonly ImmutableArray<Type> NumberDependencyTypes =
    [
        typeof(AbilityEffect.Damage), typeof(AbilityEffect.AttackDamage),
        typeof(AbilityEffect.IndirectDamage), typeof(AbilityEffect.PlaceThreat),
        typeof(AbilityEffect.ModifyPlayedEvent), typeof(AbilityEffect.PreventThreat), typeof(AbilityEffect.PreventDamage),
        typeof(AbilityEffect.GrantControlledCharacters),
        typeof(AbilityEffect.DiscardAtRandom), typeof(AbilityEffect.DiscardTop),
        typeof(AbilityPricingEffect.ReduceNextCardCost), typeof(AbilityEffect.DiscardHandCards),
        typeof(AbilityEffect.DiscardResourceIcons),
    ];

    // This engine-owned preflight collects dependencies of singular lookups,
    // including those in alternatives. Plural selections do not constrain an
    // earlier choice: their matching cards can be selected after it resolves.
    internal static void Collect(
        AbilityEffect? effect, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        if (effect is null) return;
        if (effect is AbilityEffect.ResolveInvocation or AbilityEffect.DiscardInvocation or AbilityEffect.ReturnInvocation)
        {
            areas.Add(DeckType.AdditionalDeck);
            return;
        }
        Type type = effect.GetType();
        if (OrderedContainerTypes.Contains(type))
            CollectOrderedContainer(effect, context, areas);
        else if (BranchingContainerTypes.Contains(type))
            CollectBranchingContainer(effect, context, areas);
        else if (WrappedContainerTypes.Contains(type))
            CollectWrappedContainer(effect, context, areas);
        else if (SelectionDependencyTypes.Contains(type))
            AbilitySelectionAreaDependencies.Collect(effect, context, areas);
        else if (CardMutationDependencyTypes.Contains(type))
            CollectCardMutationDependencies(effect, context, areas);
        else if (NumberDependencyTypes.Contains(type))
            CollectNumberDependencies(effect, context, areas);
        else if (!HasNoAreaDependencies(effect))
            throw new InvalidOperationException(
                "Unknown compiled effect in area-dependency analysis");
    }

    private static void CollectOrderedContainer(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        IEnumerable<AbilityEffect> children = effect switch
        {
            AbilityEffect.Sequence sequence => sequence.Effects,
            AbilityEffect.Simultaneous simultaneous => simultaneous.Effects,
            AbilityEffect.Choose choose => choose.Options,
            _ => throw new InvalidOperationException("Unknown ordered area container"),
        };
        foreach (var child in children) Collect(child, context, areas);
    }

    private static void CollectBranchingContainer(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        switch (effect)
        {
            case AbilityEffect.Conditional conditional:
                CollectSingularAreaDependencies(conditional.Test, context, areas);
                Collect(conditional.Then, context, areas);
                Collect(conditional.Else, context, areas);
                break;
            case AbilityEffect.Dependent dependent:
                Collect(dependent.Effect, context, areas);
                Collect(dependent.Continuation, context, areas);
                break;
            case AbilityEffect.EachTime each:
                Collect(each.Effect, context, areas);
                CollectSingularAreaDependencies(each.When, context, areas);
                Collect(each.Then, context, areas);
                break;
            default:
                throw new InvalidOperationException("Unknown branching area container");
        }
    }

    private static void CollectWrappedContainer(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        switch (effect)
        {
            case AbilityEffect.EachPlayer each:
                Collect(each.Effect, context, areas);
                break;
            case AbilityEffect.ForEach repeated:
                CollectSingularAreaDependencies(repeated.Count, context, areas);
                Collect(repeated.Effect, context, areas);
                break;
            case AbilityEffect.ChooseCard choose:
                Collect(choose.Effect, context, areas);
                break;
            case AbilityEffect.AfterActivation after:
                Collect(after.Effect, context, areas);
                break;
            case AbilityEffect.Power power:
                Collect(power.Effect, context, areas);
                break;
            case AbilityEffect.ThwartGroup group:
                Collect(group.Thwart, context, areas);
                break;
            case AbilityEffect.PayOrEffect payment:
                Collect(payment.Otherwise, context, areas);
                break;
            default:
                throw new InvalidOperationException("Unknown wrapped area container");
        }
    }

    private static void CollectCardMutationDependencies(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        switch (effect)
        {
            case AbilityEffect.AttachCards attach:
                CollectCardsInDependencies(attach.Host, context, areas);
                CollectCardsInDependencies(attach.Cards, context, areas);
                break;
            case AbilityEffect.StoreBeneath store:
                CollectCardsInDependencies(store.Host, context, areas);
                break;
            case AbilityEffect.DelayedDiscard delayed:
                CollectCardsInDependencies(delayed.Card, context, areas);
                break;
            case AbilityEffect.DealEncounterCard deal:
                CollectCardsInDependencies(deal.Card, context, areas);
                break;
            case AbilityEffect.PlaceAtRandom place:
                CollectCardsInDependencies(place.Host, context, areas);
                CollectSingularAreaDependencies(place.Count, context, areas);
                break;
            case AbilityEffect.Search search:
                areas.UnionWith(search.Areas.Select(AbilitySelectorEvaluation.AreaType));
                break;
            case AbilityEffect.PutIntoPlay entering:
                CollectCardsInDependencies(entering.Card, context, areas);
                break;
            case AbilityEffect.PlaceCounters counters:
                CollectCardsInDependencies(counters.Card, context, areas);
                CollectSingularAreaDependencies(counters.Count, context, areas);
                break;
            case AbilityEffect.RemoveCounters counters:
                CollectCardsInDependencies(counters.Card, context, areas);
                break;
            default:
                throw new InvalidOperationException("Unknown card-mutation area dependency");
        }
    }

    private static void CollectNumberDependencies(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        AbilityNumber number = effect switch
        {
            AbilityEffect.Damage damage => damage.Amount,
            AbilityEffect.AttackDamage damage => damage.Amount,
            AbilityEffect.IndirectDamage damage => damage.Amount,
            AbilityEffect.PlaceThreat threat => threat.Amount,
            AbilityEffect.PreventThreat threat => threat.Amount,
            AbilityEffect.PreventDamage damage => damage.Amount,
            AbilityEffect.GrantControlledCharacters grant => grant.Amount,
            AbilityEffect.DiscardHandCards discard => discard.Count,
            AbilityEffect.DiscardResourceIcons discard => discard.Count,
            AbilityEffect.DiscardAtRandom discard => discard.Count,
            AbilityEffect.DiscardTop discard => discard.Count,
            AbilityPricingEffect.ReduceNextCardCost reduction => reduction.Amount,
            _ => throw new InvalidOperationException("Unknown numeric area dependency"),
        };
        CollectSingularAreaDependencies(number, context, areas);
    }

    private static bool HasNoAreaDependencies(AbilityEffect effect) =>
        effect switch
        {
            AbilityEffect.ReplaceStatus or AbilityEffect.AttackAnotherEnemy or AbilityEffect.GiveStatus or AbilityEffect.RemoveStatuses or AbilityEffect.ChangeForm or AbilityEffect.Draw
                or AbilityEffect.DrawToHandSize or AbilityEffect.GainSurge
                or AbilityEffect.Fixed or AbilityEffect.Generate
                or AbilityEffect.InitializeAuxiliaryDeck or AbilityEffect.RevealAuxiliaryTop
                or AbilityEffect.ChooseCardsToHand or AbilityEffect.ReleaseBeneath or AbilityEffect.AllowPlayBeneath or AbilityEffect.AllowPlayFromDiscard or AbilityPricingEffect.ReduceSelfCostPerEngagedMinion or AbilityPricingEffect.ReduceSelfCostPerControlledTrait or AbilityPricingEffect.ReduceFirstAllyCost or AbilityEffect.DoubleResourceFor or AbilityEffect.Shuffle
                or AbilityEffect.PreventDamageFrom or AbilityEffect.DelayedStun
                or AbilityEffect.DealEncounterCards or AbilityEffect.EngageTopAsMinion
                or AbilityEffect.DiscardUntil or AbilityEffect.ShuffleInto
                // Inspection reads the deck only at its own prompt; admission
                // neither binds a hidden top card nor predicts its numeric result.
                or AbilityEffect.InspectEncounterTop
                or AbilityEffect.ChooseTopForHand
                or AbilityEffect.ChooseDiscardToShuffle
                or AbilityEffect.DiscardHandWithResource or AbilityEffect.DiscardHandOfKind
                or AbilityEffect.RecoverDiscardedByResource or AbilityEffect.RecruitDiscarded
                or AbilityEffect.ActivateEnemies or AbilityEffect.GrantTraitCharacters or AbilityEffect.ModifyPlayedEvent or AbilityEffect.BoostBasicUse or AbilityPricingEffect.IncreaseNextEventCost => true,
            _ => false,
        };

}
