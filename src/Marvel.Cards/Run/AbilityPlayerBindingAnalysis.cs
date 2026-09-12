using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

// Printed “you/your” binding classification used by trigger permission.
internal static class AbilityPlayerBindingAnalysis
{
    internal static bool UsesYouOrYour(
        AbilityProgram program, CompiledCardAbility ability,
        Marvel.Rules.State.Card card) =>
        ability.Trigger.Subject == AbilitySubjects.You
        || ability.Trigger.Actor == AbilityRoles.You
        || ability.Trigger.Target == AbilityRoles.You
        || ability.Trigger.Player == AbilityPlayers.You
        || Contains(ability.Effect)
        || Contains(ability.Cost)
        || ability.When is { } when && Contains(when)
        || program.AttachTo.GetValueOrDefault(card.FaceId) is { } attachment
            && Contains(attachment);

    internal static bool Contains(AbilityCost? cost) => cost switch
    {
        null => false,
        AbilityCost.Sequence sequence => sequence.Costs.Any(Contains),
        AbilityCost.Exhaust exhaust => exhaust.Card == AbilityCostCard.Identity,
        AbilityCost.Discard discard => discard.Card == AbilityCostCard.Identity,
        AbilityCost.RemoveCounters counters => counters.Card == AbilityCostCard.Identity,
        AbilityCost.Heal heal => heal.Card == AbilityCostCard.Identity,
        AbilityCost.Damage damage => damage.Card == AbilityCostCard.Identity,
        AbilityCost.ExhaustChosen chosen => chosen.From is
            AbilityCardQuery.CharactersYouControl or AbilityCardQuery.AlliesYouControl,
        AbilityCost.DiscardFromHand discard => discard.Range is AbilityCostRange.Any,
        AbilityCost.Spend or AbilityCost.SpendEnergy => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled cost in player-binding analysis"),
    };

    internal static bool Contains(AbilityEffect? effect)
    {
        if (effect is null) return false;
        return AbilityEffectBindingAnalysis.GroupOf(effect) switch
        {
            AbilityEffectBindingAnalysis.EffectGroup.Ordered => ContainsOrdered(effect),
            AbilityEffectBindingAnalysis.EffectGroup.Branching => ContainsBranching(effect),
            AbilityEffectBindingAnalysis.EffectGroup.Wrapped => ContainsWrapped(effect),
            AbilityEffectBindingAnalysis.EffectGroup.BasicCard => ContainsBasicCard(effect),
            AbilityEffectBindingAnalysis.EffectGroup.ThreatAndGrant =>
                ContainsThreatAndGrant(effect),
            AbilityEffectBindingAnalysis.EffectGroup.Player => ContainsPlayer(effect),
            AbilityEffectBindingAnalysis.EffectGroup.Placement => ContainsPlacement(effect),
            AbilityEffectBindingAnalysis.EffectGroup.Power => ContainsPower(effect),
            _ => ContainsOther(effect),
        };
    }

    private static bool ContainsOrdered(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(Contains),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(Contains),
        AbilityEffect.Choose choose => choose.Options.Any(Contains),
        _ => throw new InvalidOperationException("Unknown ordered player-binding effect"),
    };

    private static bool ContainsBranching(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Conditional conditional => Contains(conditional.Test)
            || Contains(conditional.Then) || Contains(conditional.Else),
        AbilityEffect.Dependent dependent => Contains(dependent.Effect)
            || Contains(dependent.Continuation),
        AbilityEffect.EachTime each => Contains(each.Effect)
            || Contains(each.When) || Contains(each.Then),
        _ => throw new InvalidOperationException("Unknown branching player-binding effect"),
    };

    private static bool ContainsWrapped(AbilityEffect effect) => effect switch
    {
        AbilityEffect.EachPlayer each => Contains(each.Effect),
        AbilityEffect.ForEach repeated => Contains(repeated.Count) || Contains(repeated.Effect),
        AbilityEffect.ChooseCard choose => Contains(choose.From) || Contains(choose.Effect),
        AbilityEffect.AfterActivation after => Contains(after.Effect),
        AbilityEffect.PayOrEffect payment => Contains(payment.Otherwise),
        _ => throw new InvalidOperationException("Unknown wrapped player-binding effect"),
    };

    private static bool ContainsBasicCard(AbilityEffect effect) => effect switch
    {
        AbilityEffect.CardAction action => Contains(action.Selection),
        AbilityEffect.Heal heal => Contains(heal.Card) || Contains(heal.Amount),
        AbilityEffect.Damage damage => Contains(damage.Cards) || Contains(damage.Amount),
        AbilityEffect.AttackDamage damage => Contains(damage.Cards) || Contains(damage.Amount),
        AbilityEffect.MoveDamage move => Contains(move.From) || Contains(move.To) || Contains(move.Amount),
        AbilityEffect.IndirectDamage damage => Contains(damage.Among) || Contains(damage.Amount),
        AbilityEffect.GiveStatus status => Contains(status.Cards),
        _ => throw new InvalidOperationException("Unknown basic-card player-binding effect"),
    };

    private static bool ContainsThreatAndGrant(AbilityEffect effect) => effect switch
    {
        AbilityEffect.PlaceThreat threat => Contains(threat.Schemes) || Contains(threat.Amount),
        AbilityEffect.RemoveThreat threat => Contains(threat.Schemes) || Contains(threat.Amount)
            || threat.OverridesCannotFrom is { } source && Contains(source),
        AbilityEffect.PreventThreat threat => Contains(threat.Amount),
        AbilityEffect.PreventDamage damage => Contains(damage.Amount),
        AbilityEffect.GrantTrait grant => Contains(grant.Cards),
        AbilityEffect.GrantField grant => Contains(grant.Cards) || Contains(grant.Amount),
        AbilityEffect.PreventDamageWhile prevention => Contains(prevention.Condition),
        _ => throw new InvalidOperationException("Unknown threat player-binding effect"),
    };

    private static bool ContainsPlayer(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ChangeForm form => form.Player == AbilityPlayer.You,
        AbilityEffect.Draw draw => Contains(draw.Players),
        AbilityEffect.DrawToHandSize draw => draw.Player == AbilityPlayer.You,
        AbilityEffect.GrantControlledCharacters grant =>
            grant.Player == AbilityPlayer.You || Contains(grant.Amount),
        AbilityEffect.DealEncounterCards deal => Contains(deal.Players),
        AbilityEffect.CreateDrones create => Contains(create.Players),
        AbilityEffect.DealEncounterCard deal =>
            Contains(deal.Card) || deal.Player == AbilityPlayer.You,
        _ => throw new InvalidOperationException("Unknown player-target player-binding effect"),
    };

    private static bool ContainsPlacement(AbilityEffect effect) => effect switch
    {
        AbilityEffect.DelayedDiscard delayed => Contains(delayed.Card),
        AbilityEffect.DiscardAtRandom discard => Contains(discard.Players) || Contains(discard.Count),
        AbilityEffect.PlaceAtRandom place =>
            Contains(place.Players) || Contains(place.Count) || Contains(place.Host),
        AbilityEffect.DiscardTop discard => discard.From == AbilitySearchArea.YourDeck
            || discard.Players is { } players && Contains(players) || Contains(discard.Count),
        AbilityEffect.ShuffleInto shuffle =>
            Contains(shuffle.Cards) || shuffle.Deck == AbilitySearchArea.YourDeck,
        AbilityEffect.PutIntoPlay entering =>
            !entering.PrintedDestination || Contains(entering.Card),
        AbilityEffect.PlaceCounters counters => Contains(counters.Card) || Contains(counters.Count),
        AbilityEffect.RemoveCounters counters => Contains(counters.Card),
        _ => throw new InvalidOperationException("Unknown placement player-binding effect"),
    };

    private static bool ContainsPower(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ReduceNextCardCost reduction =>
            reduction.Player == AbilityPlayer.You || Contains(reduction.Amount),
        AbilityEffect.Power power =>
            power.Target is { } target && Contains(target) || Contains(power.Effect),
        AbilityEffect.ThwartGroup thwart => Contains(thwart.Schemes) || Contains(thwart.Thwart),
        AbilityEffect.ActivateEnemies activate => Contains(activate.Enemies)
            || activate.Against is { } target && Contains(target),
        _ => throw new InvalidOperationException("Unknown power player-binding effect"),
    };

    private static bool ContainsOther(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Shuffle shuffle => shuffle.Area == AbilitySearchArea.YourDeck,
        AbilityEffect.Search search => search.Areas.Contains(AbilitySearchArea.YourDeck),
        AbilityEffect.GainSurge or AbilityEffect.Fixed or AbilityEffect.Generate
            or AbilityEffect.DoubleResourceFor or AbilityEffect.PreventDamageFrom
            or AbilityEffect.DelayedStun or AbilityEffect.DiscardUntil
            or AbilityEffect.ChooseTopForHand or AbilityEffect.ChooseDiscardToShuffle
            or AbilityEffect.DiscardHandWithResource
            or AbilityEffect.RecoverDiscardedByResource => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled effect in player-binding analysis"),
    };

    internal static bool Contains(AbilityPlayerSelection players) => players switch
    {
        AbilityPlayerSelection.OnePlayer one => one.Player == AbilityPlayer.You,
        AbilityPlayerSelection.AllPlayers => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled player selection in player-binding analysis"),
    };

    internal static bool Contains(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.Bound bound => bound.Binding is
            AbilityCardBinding.You or AbilityCardBinding.YourHero
            or AbilityCardBinding.YourAlterEgo,
        AbilityCardSelection.Query query => query.Kind is
            AbilityCardQuery.YourAsideMinion or AbilityCardQuery.YourAsideSideScheme
            or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.YourAsidePile
            or AbilityCardQuery.UpgradesAndSupportsYouControl
            or AbilityCardQuery.IdentitySpecificInYourHand
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.CharactersYouControl
            or AbilityCardQuery.UpgradesYouControl or AbilityCardQuery.AlliesYouControl
            or AbilityCardQuery.DronesEngagedWithYou,
        AbilityCardSelection.WithTrait trait => Contains(trait.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => Contains(unoccupied.Cards),
        AbilityCardSelection.Discardable discardable => Contains(discardable.Cards),
        AbilityCardSelection.Ranked ranked => Contains(ranked.Cards),
        AbilityCardSelection.InAreas areas => areas.Areas.Contains(AbilitySearchArea.YourDeck),
        AbilityCardSelection.Titled or AbilityCardSelection.EnemiesWithTrait => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled selector in player-binding analysis"),
    };

    internal static bool Contains(AbilityNumber number) => number switch
    {
        AbilityNumber.Sum sum => sum.Operands.Any(Contains),
        AbilityNumber.Product product => product.Operands.Any(Contains),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(Contains),
        AbilityNumber.CardValue value => Contains(value.Card),
        AbilityNumber.Counters counters => Contains(counters.Card),
        AbilityNumber.Modified modified => Contains(modified.Card),
        AbilityNumber.Count count => Contains(count.Cards),
        AbilityNumber.Conditional conditional => Contains(conditional.Test)
            || Contains(conditional.Then) || Contains(conditional.Else),
        AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
            or AbilityNumber.PrintedResourcesDiscarded
            or AbilityNumber.DiscardedWithResource or AbilityNumber.ResolutionValue => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled number in player-binding analysis"),
    };

    internal static bool Contains(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.Any(Contains),
        AbilityCondition.Any any => any.Operands.Any(Contains),
        AbilityCondition.Negated negated => Contains(negated.Operand),
        AbilityCondition.Flag flag => flag.Kind is AbilityConditionFact.DefeatedByYou
            or AbilityConditionFact.HeroDefended or AbilityConditionFact.UndefendedAttack,
        AbilityCondition.Exists exists => Contains(exists.Cards),
        AbilityCondition.LegalPractice practice => Contains(practice.Schemes),
        AbilityCondition.AutomaticThwart thwart => Contains(thwart.Scheme),
        AbilityCondition.AtLeast comparison => Contains(comparison.Value) || Contains(comparison.Count),
        AbilityCondition.InForm form => form.Player == AbilityPlayer.You,
        AbilityCondition.CardText text => Contains(text.Card),
        AbilityCondition.IsKind kind => Contains(kind.Card),
        AbilityCondition.WasDefeated defeated => Contains(defeated.Card),
        AbilityCondition.IsYourIdentity => true,
        AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource
            or AbilityCondition.CausedThreat or AbilityCondition.TitleInPlay
            or AbilityCondition.ActivationIs => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled condition in player-binding analysis"),
    };
}
