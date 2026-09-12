using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityAdmissionAreaDependencies
{
    private static readonly HashSet<Type> OrderedContainerTypes =
    [
        typeof(AbilityEffect.Sequence), typeof(AbilityEffect.Simultaneous),
        typeof(AbilityEffect.Choose),
    ];

    private static readonly HashSet<Type> BranchingContainerTypes =
    [
        typeof(AbilityEffect.Conditional), typeof(AbilityEffect.Dependent),
        typeof(AbilityEffect.EachTime),
    ];

    private static readonly HashSet<Type> WrappedContainerTypes =
    [
        typeof(AbilityEffect.EachPlayer), typeof(AbilityEffect.ForEach),
        typeof(AbilityEffect.ChooseCard), typeof(AbilityEffect.AfterActivation),
        typeof(AbilityEffect.Power), typeof(AbilityEffect.ThwartGroup),
        typeof(AbilityEffect.PayOrEffect),
    ];

    private static readonly HashSet<Type> SelectionDependencyTypes =
    [
        typeof(AbilityEffect.CardAction), typeof(AbilityEffect.Heal),
        typeof(AbilityEffect.MoveDamage), typeof(AbilityEffect.RemoveThreat),
        typeof(AbilityEffect.GrantTrait), typeof(AbilityEffect.GrantField),
        typeof(AbilityEffect.PreventDamageWhile),
    ];

    private static readonly HashSet<Type> CardMutationDependencyTypes =
    [
        typeof(AbilityEffect.DelayedDiscard), typeof(AbilityEffect.DealEncounterCard),
        typeof(AbilityEffect.PlaceAtRandom), typeof(AbilityEffect.Search),
        typeof(AbilityEffect.PutIntoPlay), typeof(AbilityEffect.PlaceCounters),
        typeof(AbilityEffect.RemoveCounters),
    ];

    private static readonly HashSet<Type> NumberDependencyTypes =
    [
        typeof(AbilityEffect.Damage), typeof(AbilityEffect.AttackDamage),
        typeof(AbilityEffect.IndirectDamage), typeof(AbilityEffect.PlaceThreat),
        typeof(AbilityEffect.PreventThreat), typeof(AbilityEffect.PreventDamage),
        typeof(AbilityEffect.GrantControlledCharacters),
        typeof(AbilityEffect.DiscardAtRandom), typeof(AbilityEffect.DiscardTop),
        typeof(AbilityEffect.ReduceNextCardCost),
    ];

    private static readonly HashSet<AbilityCardInstruction> SingularCardInstructions =
    [
        AbilityCardInstruction.AttachTo, AbilityCardInstruction.Exhaust,
        AbilityCardInstruction.RemoveFromGame, AbilityCardInstruction.Reveal,
        AbilityCardInstruction.ReturnToHand, AbilityCardInstruction.AddToHand,
        AbilityCardInstruction.ReturnOwnedToHand, AbilityCardInstruction.Discard,
        AbilityCardInstruction.SoakDamage,
    ];

    // This engine-owned preflight collects dependencies of singular lookups,
    // including those in alternatives. Plural selections do not constrain an
    // earlier choice: their matching cards can be selected after it resolves.
    internal static void Collect(
        AbilityEffect? effect, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        if (effect is null) return;
        Type type = effect.GetType();
        if (OrderedContainerTypes.Contains(type))
            CollectOrderedContainer(effect, context, areas);
        else if (BranchingContainerTypes.Contains(type))
            CollectBranchingContainer(effect, context, areas);
        else if (WrappedContainerTypes.Contains(type))
            CollectWrappedContainer(effect, context, areas);
        else if (SelectionDependencyTypes.Contains(type))
            CollectSelectionDependencies(effect, context, areas);
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

    private static void CollectSelectionDependencies(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        switch (effect)
        {
            case AbilityEffect.CardAction action:
                if (SingularCardInstructions.Contains(action.Instruction))
                    CollectCardsInDependencies(action.Selection, context, areas);
                break;
            case AbilityEffect.Heal heal:
                CollectCardsInDependencies(heal.Card, context, areas);
                CollectSingularAreaDependencies(heal.Amount, context, areas);
                break;
            case AbilityEffect.MoveDamage move:
                CollectCardsInDependencies(move.From, context, areas);
                CollectCardsInDependencies(move.To, context, areas);
                CollectSingularAreaDependencies(move.Amount, context, areas);
                break;
            case AbilityEffect.RemoveThreat threat:
                CollectCardsInDependencies(threat.Schemes, context, areas);
                CollectSingularAreaDependencies(threat.Amount, context, areas);
                break;
            case AbilityEffect.GrantTrait or AbilityEffect.GrantField:
                CollectGrantDependencies(effect, context, areas);
                break;
            case AbilityEffect.PreventDamageWhile prevention:
                CollectSingularAreaDependencies(prevention.Condition, context, areas);
                break;
            default:
                throw new InvalidOperationException("Unknown selection area dependency");
        }
    }

    private static void CollectGrantDependencies(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        if (effect is AbilityEffect.GrantTrait trait)
        {
            if (trait.Until is not null)
                CollectCardsInDependencies(trait.Cards, context, areas);
            return;
        }
        var field = (AbilityEffect.GrantField)effect;
        if (field.Until is not null)
            CollectCardsInDependencies(field.Cards, context, areas);
        CollectSingularAreaDependencies(field.Amount, context, areas);
    }

    private static void CollectCardMutationDependencies(
        AbilityEffect effect, AbilityAdmissionContext context,
        HashSet<DeckType> areas)
    {
        switch (effect)
        {
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
                areas.UnionWith(search.Areas.Select(area => Area(area, context).Type));
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
            AbilityEffect.DiscardAtRandom discard => discard.Count,
            AbilityEffect.DiscardTop discard => discard.Count,
            AbilityEffect.ReduceNextCardCost reduction => reduction.Amount,
            _ => throw new InvalidOperationException("Unknown numeric area dependency"),
        };
        CollectSingularAreaDependencies(number, context, areas);
    }

    private static bool HasNoAreaDependencies(AbilityEffect effect) =>
        effect switch
        {
            AbilityEffect.GiveStatus or AbilityEffect.ChangeForm or AbilityEffect.Draw
                or AbilityEffect.DrawToHandSize or AbilityEffect.GainSurge
                or AbilityEffect.Fixed or AbilityEffect.Generate
                or AbilityEffect.DoubleResourceFor or AbilityEffect.Shuffle
                or AbilityEffect.PreventDamageFrom or AbilityEffect.DelayedStun
                or AbilityEffect.DealEncounterCards or AbilityEffect.CreateDrones
                or AbilityEffect.DiscardUntil or AbilityEffect.ShuffleInto
                or AbilityEffect.ChooseTopForHand
                or AbilityEffect.ChooseDiscardToShuffle
                or AbilityEffect.DiscardHandWithResource
                or AbilityEffect.RecoverDiscardedByResource
                or AbilityEffect.ActivateEnemies => true,
            _ => false,
        };

    private static void CollectSingularAreaDependencies(
        AbilityCondition condition, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        var cast = context;
        switch (condition)
        {
            case AbilityCondition.All all:
                foreach (var operand in all.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityCondition.Any any:
                foreach (var operand in any.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityCondition.Negated negated:
                CollectSingularAreaDependencies(negated.Operand, cast, areas);
                break;
            case AbilityCondition.AtLeast comparison:
                CollectSingularAreaDependencies(comparison.Value, cast, areas);
                CollectSingularAreaDependencies(comparison.Count, cast, areas);
                break;
            case AbilityCondition.CardText text: CollectCardsInDependencies(text.Card, cast, areas); break;
            case AbilityCondition.IsKind kind: CollectCardsInDependencies(kind.Card, cast, areas); break;
            case AbilityCondition.WasDefeated defeated: CollectCardsInDependencies(defeated.Card, cast, areas); break;
            case AbilityCondition.Flag or AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource
                or AbilityCondition.CausedThreat or AbilityCondition.Exists or AbilityCondition.LegalPractice
                or AbilityCondition.AutomaticThwart or AbilityCondition.TitleInPlay or AbilityCondition.InForm
                or AbilityCondition.ActivationIs or AbilityCondition.IsYourIdentity:
                break;
            default: throw new InvalidOperationException("Unknown compiled condition in area-dependency analysis");
        }
    }

    private static void CollectSingularAreaDependencies(
        AbilityNumber number, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        var cast = context;
        switch (number)
        {
            case AbilityNumber.Sum sum:
                foreach (var operand in sum.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityNumber.Product product:
                foreach (var operand in product.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityNumber.Minimum minimum:
                foreach (var operand in minimum.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityNumber.Conditional conditional:
                CollectSingularAreaDependencies(conditional.Test, cast, areas);
                CollectSingularAreaDependencies(conditional.Then, cast, areas);
                CollectSingularAreaDependencies(conditional.Else, cast, areas);
                break;
            case AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
                or AbilityNumber.CardValue or AbilityNumber.Counters or AbilityNumber.Modified
                or AbilityNumber.Count or AbilityNumber.PrintedResourcesDiscarded
                or AbilityNumber.DiscardedWithResource or AbilityNumber.ResolutionValue:
                break;
            default: throw new InvalidOperationException("Unknown compiled number in area-dependency analysis");
        }
    }

    private static void CollectCardsInDependencies(
        AbilityCardSelection selector, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        var cast = context;
        switch (selector)
        {
            case AbilityCardSelection.InAreas selection:
                areas.UnionWith(selection.Areas.Select(area => Area(area, cast).Type));
                break;
            case AbilityCardSelection.WithTrait filtered: CollectCardsInDependencies(filtered.Cards, cast, areas); break;
            case AbilityCardSelection.WithoutAnotherCopyAttached filtered: CollectCardsInDependencies(filtered.Cards, cast, areas); break;
            case AbilityCardSelection.Discardable filtered: CollectCardsInDependencies(filtered.Cards, cast, areas); break;
            case AbilityCardSelection.Ranked ranked: CollectCardsInDependencies(ranked.Cards, cast, areas); break;
            case AbilityCardSelection.Bound or AbilityCardSelection.Query or AbilityCardSelection.Titled
                or AbilityCardSelection.EnemiesWithTrait:
                break;
            default: throw new InvalidOperationException("Unknown compiled selector in area-dependency analysis");
        }
    }

    private static Area Area(AbilitySearchArea area, AbilityAdmissionContext context) => area switch
    {
        AbilitySearchArea.EncounterDeck => context.World.AreaOf(DeckType.EncounterDeck),
        AbilitySearchArea.EncounterDiscardPile => context.World.AreaOf(DeckType.EncounterDiscardPile),
        AbilitySearchArea.ScenarioSetAside => context.World.AreaOf(DeckType.AsideDeck),
        AbilitySearchArea.YourDeck => context.World.Seats[context.Query.Player].Deck,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };
}
