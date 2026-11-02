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
        AbilityCost.ReturnSourceToHand or AbilityCost.ChooseScheme or AbilityCost.PlaceMainSchemeThreat => false,
        AbilityCost.ChooseFromDiscard or AbilityCost.InvocationTop => true,
        AbilityCost.DiscardTop discard => discard.From == AbilitySearchArea.YourDeck,
        AbilityCost.DiscardUntilMinionEngaged or AbilityCost.DealEncounterToYou or AbilityCost.RandomDiscardFromHand => true,
        AbilityCost.Sequence sequence => sequence.Costs.Any(Contains),
        AbilityCost.ExhaustTitled or AbilityCost.ReturnTitledToHand => true,
        AbilityCost.Exhaust exhaust => exhaust.Card == AbilityCostCard.Identity,
        AbilityCost.ConsumeCampaignCard consume => consume.Card == AbilityCostCard.Identity,
        AbilityCost.Discard discard => discard.Card == AbilityCostCard.Identity,
        AbilityCost.RemoveCounters counters => counters.Card == AbilityCostCard.Identity,
        AbilityCost.Heal heal => heal.Card == AbilityCostCard.Identity,
        AbilityCost.Damage damage => damage.Card == AbilityCostCard.Identity,
        AbilityCost.ExhaustChosen chosen => chosen.From is
            AbilityCardQuery.CharactersYouControl or AbilityCardQuery.AlliesYouControl,
        AbilityCost.DiscardFromHand discard => discard.Range is AbilityCostRange.Any,
        AbilityCost.Spend or AbilityCost.SpendX => false,
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
        AbilityEffect.CardAction action => action.Instruction is AbilityCardInstruction.PlaceOnBottomOfDeck or AbilityCardInstruction.PlaceOnTopOfDeck
            || Contains(action.Selection),
        AbilityEffect.Heal heal => Contains(heal.Card) || Contains(heal.Amount),
        AbilityEffect.SetHitPoints assignment => Contains(assignment.Card),
        AbilityEffect.Damage damage => Contains(damage.Cards) || Contains(damage.Amount),
        AbilityEffect.AttackDamage damage => Contains(damage.Cards) || Contains(damage.Amount),
        AbilityEffect.MoveThreat move => Contains(move.From) || Contains(move.To) || Contains(move.Amount),
        AbilityEffect.MoveDamage move => Contains(move.From) || Contains(move.To) || Contains(move.Amount),
        AbilityEffect.IndirectDamage damage => Contains(damage.Among) || Contains(damage.Amount),
        AbilityEffect.ReplaceStatus status => Contains(status.Card),
        AbilityEffect.GiveStatus status => Contains(status.Cards),
        AbilityEffect.RemoveStatuses status => Contains(status.Cards),
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
        AbilityEffect.DefineBaseValue definition => Contains(definition.Value),
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
        AbilityEffect.EngageTopAsMinion create => Contains(create.Players),
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
            Contains(shuffle.Cards) || shuffle.Deck == AbilityShuffleDestination.YourDeck,
        AbilityEffect.PutIntoPlay entering =>
            entering is { PrintedDestination: false, FirstPlayer: false } || Contains(entering.Card),
        AbilityCounterEffect counters => ContainsCounters(counters),
        _ => throw new InvalidOperationException("Unknown placement player-binding effect"),
    };

    private static bool ContainsCounters(AbilityCounterEffect effect) => effect switch
    {
        AbilityCounterEffect.PlaceCounters counters => Contains(counters.Card) || Contains(counters.Count),
        AbilityCounterEffect.RemoveCounters counters => Contains(counters.Card),
        AbilityCounterEffect.MoveCounters move => Contains(move.From) || Contains(move.To),
        _ => throw new InvalidOperationException("Unknown counter player-binding effect"),
    };

    private static bool ContainsPower(AbilityEffect effect) => effect switch
    {
        AbilityPricingEffect.ReduceNextCardCost reduction =>
            reduction.Player == AbilityPlayer.You || Contains(reduction.Amount),
        AbilityEffect.Power power =>
            power.Target is { } target && Contains(target) || Contains(power.Effect),
        AbilityEffect.ThwartGroup thwart => Contains(thwart.Schemes) || Contains(thwart.Thwart),
        AbilityEffect.ActivateEnemies activate => Contains(activate.Enemies)
            || activate.Against is { } target && Contains(target)
            || activate.Modifier is { } modifier && Contains(modifier),
        _ => throw new InvalidOperationException("Unknown power player-binding effect"),
    };

    private static bool ContainsOther(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ResolveInvocation or AbilityEffect.DiscardInvocation or AbilityEffect.ReturnInvocation => true,
        AbilityEffect.DiscardHandCards => true,
        AbilityEffect.DiscardResourceIcons => true,
        AbilityEffect.RecruitDiscarded => true,
        AbilityEffect.DiscardUntil discard => discard.Player == AbilityPlayer.You,
        AbilityEffect.AttachCards attach => Contains(attach.Cards) || Contains(attach.Host),
        AbilityEffect.ChooseCardsToHand group => Contains(group.From),
        AbilityEffect.ExhaustAnyNumber exhaust => Contains(exhaust.From),
        AbilityEffect.ReleaseBeneath or AbilityPricingEffect.IncreaseNextEventCost => false,
        AbilityEnemyDamageEffect.HealOnEnemyDamage or AbilityEnemyDamageEffect.IncreaseExcessDamage => false,
        AbilityEffect.StoreBeneath store => Contains(store.Cards) || Contains(store.Host),
        AbilityEffect.StoreDeckTopBeneath => true,
        AbilityEffect.InitializeAuxiliaryDeck initialize => Contains(initialize.Cards),
        AbilityEffect.RevealAuxiliaryTop => false,
        AbilityEffect.Shuffle shuffle => shuffle.Area == AbilitySearchArea.YourDeck,
        AbilityEffect.Search search => search.Areas.Contains(AbilitySearchArea.YourDeck),
        AbilityEffect.DefineProfile or AbilityEffect.GainSurge or AbilityEffect.Fixed or AbilityEffect.Generate
            or AbilityEffect.AllowPlayBeneath or AbilityEffect.AllowPlayFromDiscard or AbilityPricingEffect.ReduceSelfCostPerEngagedMinion or AbilityPricingEffect.ReduceSelfCostPerControlledTrait or AbilityPricingEffect.ReduceFirstAllyCost or AbilityPricingEffect.ReduceUpgradeCostOnThis or AbilityEffect.DoubleResourceFor or AbilityEffect.PreventDamageFrom
            or AbilityEffect.DelayedStun
            or AbilityEffect.AttackAnotherEnemy or AbilityEffect.ChooseTopForHand or AbilityEffect.ChooseDiscardToShuffle
            or AbilityEffect.DiscardHandWithResource or AbilityEffect.DiscardHandOfKind
            or AbilityEffect.RecoverDiscardedByResource or AbilityEffect.GrantTraitCharacters or AbilityEffect.ModifyPlayedEvent or AbilityEffect.BoostBasicUse => false,
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
            or AbilityCardQuery.VillainAndMinionsEngagedWithYou or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.YourAsidePile
            or AbilityCardQuery.UpgradesAndSupportsYouControl or AbilityCardQuery.CardsYouControl
            or AbilityCardQuery.AlliesAndSupportsYouControl
            or AbilityCardQuery.IdentitySpecificInYourHand
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.CharactersYouControl
            or AbilityCardQuery.CharactersYouControlExceptCost
            or AbilityCardQuery.UpgradesYouControl or AbilityCardQuery.AlliesYouControl
           ,
        AbilityCardSelection.StatusCardsOn statuses => Contains(new AbilityCardSelection.Bound(statuses.Host)),
        AbilityCardSelection.WithTrait trait => Contains(trait.Cards),
        AbilityCardSelection.WithStatus status => Contains(status.Cards),
        AbilityCardSelection.FaceDown trait => Contains(trait.Cards),
        AbilityCardSelection.FirstAvailable fallback => fallback.Options.Any(Contains),
        AbilityCardSelection.Last trait => Contains(trait.Cards),
        AbilityCardSelection.InObjectIdOrder trait => Contains(trait.Cards),
        AbilityCardSelection.WithMatchingPlayerArea trait => Contains(trait.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => Contains(unoccupied.Cards),
        AbilityCardSelection.Discardable discardable => Contains(discardable.Cards),
        AbilityCardSelection.Ranked ranked => Contains(ranked.Cards),
        AbilityCardSelection.InPlayerArea area => area.Player is AbilityPlayer.You or AbilityPlayer.Controller,
        AbilityCardSelection.DefeatedAllyInYourDiscardPile => true,
        AbilityCardSelection.InAreas areas => areas.Areas.Any(area => area is AbilitySearchArea.YourDeck or AbilitySearchArea.YourDiscardPile or AbilitySearchArea.YourHand or AbilitySearchArea.YourPlayArea),
        AbilityCardSelection.DefeatedWithProfile or AbilityCardSelection.Titled or AbilityCardSelection.EnemiesWithTrait => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled selector in player-binding analysis"),
    };

    internal static bool Contains(AbilityNumber number) => number switch
    {
        AbilityNumber.Sum sum => sum.Operands.Any(Contains),
        AbilityNumber.Product product => product.Operands.Any(Contains),
        AbilityNumber.FloorDivide division => division.Operands.Any(Contains),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(Contains),
        AbilityNumber.CardValue value => Contains(value.Card),
        AbilityNumber.Counters counters => Contains(counters.Card),
        AbilityNumber.Modified modified => Contains(modified.Card),
        AbilityNumber.Count count => Contains(count.Cards),
        AbilityNumber.SumPrintedCost sum => Contains(sum.Cards),
        AbilityNumber.Conditional conditional => Contains(conditional.Test)
            || Contains(conditional.Then) || Contains(conditional.Else),
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.ActivationAttacksAgainstYou } => true,
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
            or AbilityConditionFact.HeroDefended or AbilityConditionFact.HeroBasicDefended
            or AbilityConditionFact.UndefendedAttack,
        AbilityCondition.Exists exists => Contains(exists.Cards),
        AbilityCondition.LegalPractice practice => Contains(practice.Schemes),
        AbilityCondition.AutomaticThwart thwart => Contains(thwart.Scheme),
        AbilityCondition.AtLeast comparison => Contains(comparison.Value) || Contains(comparison.Count),
        AbilityCondition.InForm form => form.Player == AbilityPlayer.You,
        AbilityCondition.CardText text => Contains(text.Card),
        AbilityCondition.IsKind kind => Contains(kind.Card),
        AbilityCondition.IsInPlay inPlay => Contains(inPlay.Card),
        AbilityCondition.IsEnemyAttacker attacker => Contains(attacker.Card),
        AbilityCondition.WasDefeatedByDamage defeated => Contains(defeated.Card),
        AbilityCondition.WasDefeated defeated => Contains(defeated.Card),
        AbilityCondition.DefendedBy defender => Contains(defender.Card),
        AbilityCondition.IsYourIdentity => true,
        AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource or AbilityCondition.DiscardedWithTrait
            or AbilityCondition.CausedThreat or AbilityCondition.TitleInPlay or AbilityCondition.NamedInPlay
            or AbilityCondition.ActivationIs => false,
        _ => throw new InvalidOperationException(
            "Unknown compiled condition in player-binding analysis"),
    };
}
