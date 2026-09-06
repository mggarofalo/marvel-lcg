using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityAdmissionAreaDependencies
{
    // This engine-owned preflight collects dependencies of singular lookups,
    // including those in alternatives. Plural selections do not constrain an
    // earlier choice: their matching cards can be selected after it resolves.
    internal static void Collect(
        AbilityEffect? effect, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        var cast = context;
        void Visit(AbilityEffect? child) => Collect(child, cast, areas);
        void Number(AbilityNumber number) => CollectSingularAreaDependencies(number, cast, areas);
        void Condition(AbilityCondition condition) => CollectSingularAreaDependencies(condition, cast, areas);
        void Card(AbilityCardSelection card) => CollectCardsInDependencies(card, cast, areas);

        switch (effect)
        {
            case null: break;
            case AbilityEffect.Sequence sequence:
                foreach (var child in sequence.Effects) Visit(child);
                break;
            case AbilityEffect.Simultaneous simultaneous:
                foreach (var child in simultaneous.Effects) Visit(child);
                break;
            case AbilityEffect.Conditional conditional:
                Condition(conditional.Test); Visit(conditional.Then); Visit(conditional.Else);
                break;
            case AbilityEffect.Dependent dependent:
                Visit(dependent.Effect); Visit(dependent.Continuation);
                break;
            case AbilityEffect.EachPlayer each: Visit(each.Effect); break;
            case AbilityEffect.ForEach repeated:
                Number(repeated.Count); Visit(repeated.Effect);
                break;
            case AbilityEffect.EachTime each:
                Visit(each.Effect); Condition(each.When); Visit(each.Then);
                break;
            case AbilityEffect.Choose choose:
                foreach (var option in choose.Options) Visit(option);
                break;
            case AbilityEffect.ChooseCard choose: Visit(choose.Effect); break;
            case AbilityEffect.AfterActivation after: Visit(after.Effect); break;
            case AbilityEffect.Power power: Visit(power.Effect); break;
            case AbilityEffect.ThwartGroup group: Visit(group.Thwart); break;
            case AbilityEffect.PayOrEffect payment: Visit(payment.Otherwise); break;
            case AbilityEffect.CardAction action:
                if (action.Instruction is AbilityCardInstruction.AttachTo or AbilityCardInstruction.Exhaust
                    or AbilityCardInstruction.RemoveFromGame or AbilityCardInstruction.Reveal
                    or AbilityCardInstruction.ReturnToHand or AbilityCardInstruction.AddToHand
                    or AbilityCardInstruction.ReturnOwnedToHand or AbilityCardInstruction.Discard
                    or AbilityCardInstruction.SoakDamage)
                    Card(action.Selection);
                break;
            case AbilityEffect.Heal heal: Card(heal.Card); Number(heal.Amount); break;
            case AbilityEffect.Damage damage: Number(damage.Amount); break;
            case AbilityEffect.AttackDamage damage: Number(damage.Amount); break;
            case AbilityEffect.IndirectDamage damage: Number(damage.Amount); break;
            case AbilityEffect.MoveDamage move: Card(move.From); Card(move.To); Number(move.Amount); break;
            case AbilityEffect.PlaceThreat threat: Number(threat.Amount); break;
            case AbilityEffect.RemoveThreat threat: Card(threat.Schemes); Number(threat.Amount); break;
            case AbilityEffect.PreventThreat threat: Number(threat.Amount); break;
            case AbilityEffect.PreventDamage damage: Number(damage.Amount); break;
            case AbilityEffect.GrantTrait grant:
                if (grant.Until is not null) Card(grant.Cards);
                break;
            case AbilityEffect.GrantField grant:
                if (grant.Until is not null) Card(grant.Cards);
                Number(grant.Amount);
                break;
            case AbilityEffect.GrantControlledCharacters grant: Number(grant.Amount); break;
            case AbilityEffect.PreventDamageWhile prevention: Condition(prevention.Condition); break;
            case AbilityEffect.DelayedDiscard delayed: Card(delayed.Card); break;
            case AbilityEffect.DealEncounterCard deal: Card(deal.Card); break;
            case AbilityEffect.DiscardAtRandom discard: Number(discard.Count); break;
            case AbilityEffect.PlaceAtRandom place: Card(place.Host); Number(place.Count); break;
            case AbilityEffect.DiscardTop discard: Number(discard.Count); break;
            case AbilityEffect.Search search:
                areas.UnionWith(search.Areas.Select(area => Area(area, cast).Type));
                break;
            case AbilityEffect.PutIntoPlay entering: Card(entering.Card); break;
            case AbilityEffect.PlaceCounters counters: Card(counters.Card); Number(counters.Count); break;
            case AbilityEffect.RemoveCounters counters: Card(counters.Card); break;
            case AbilityEffect.ReduceNextCardCost reduction: Number(reduction.Amount); break;
            case AbilityEffect.GiveStatus or AbilityEffect.ChangeForm or AbilityEffect.Draw
                or AbilityEffect.DrawToHandSize or AbilityEffect.GainSurge or AbilityEffect.Fixed
                or AbilityEffect.Generate or AbilityEffect.DoubleResourceFor or AbilityEffect.Shuffle
                or AbilityEffect.PreventDamageFrom or AbilityEffect.DelayedStun
                or AbilityEffect.DealEncounterCards or AbilityEffect.CreateDrones or AbilityEffect.DiscardUntil
                or AbilityEffect.ShuffleInto or AbilityEffect.ChooseTopForHand or AbilityEffect.ChooseDiscardToShuffle
                or AbilityEffect.DiscardHandWithResource or AbilityEffect.RecoverDiscardedByResource
                or AbilityEffect.ActivateEnemies:
                break;
            default: throw new InvalidOperationException("Unknown compiled effect in area-dependency analysis");
        }
    }

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
