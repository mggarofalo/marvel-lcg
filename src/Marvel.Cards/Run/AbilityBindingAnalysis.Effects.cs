using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static class AbilityEffectBindingAnalysis
{
    internal enum EffectGroup
    {
        Ordered,
        Branching,
        Wrapped,
        BasicCard,
        ThreatAndGrant,
        Player,
        Placement,
        Power,
        Other,
    }

    private static readonly HashSet<Type> OrderedTypes =
    [
        typeof(AbilityEffect.Sequence), typeof(AbilityEffect.Simultaneous),
        typeof(AbilityEffect.Choose),
    ];
    private static readonly HashSet<Type> BranchingTypes =
    [
        typeof(AbilityEffect.Conditional), typeof(AbilityEffect.Dependent),
        typeof(AbilityEffect.EachTime),
    ];
    private static readonly HashSet<Type> WrappedTypes =
    [
        typeof(AbilityEffect.EachPlayer), typeof(AbilityEffect.ForEach),
        typeof(AbilityEffect.ChooseCard), typeof(AbilityEffect.AfterActivation),
        typeof(AbilityEffect.PayOrEffect),
    ];
    private static readonly HashSet<Type> BasicCardTypes =
    [
        typeof(AbilityEffect.CardAction), typeof(AbilityEffect.Heal),
        typeof(AbilityEffect.Damage), typeof(AbilityEffect.AttackDamage),
        typeof(AbilityEffect.MoveDamage), typeof(AbilityEffect.IndirectDamage),
        typeof(AbilityEffect.GiveStatus),
    ];
    private static readonly HashSet<Type> ThreatAndGrantTypes =
    [
        typeof(AbilityEffect.PlaceThreat), typeof(AbilityEffect.RemoveThreat),
        typeof(AbilityEffect.PreventThreat), typeof(AbilityEffect.PreventDamage),
        typeof(AbilityEffect.GrantTrait), typeof(AbilityEffect.GrantField),
        typeof(AbilityEffect.PreventDamageWhile),
    ];
    private static readonly HashSet<Type> PlayerTypes =
    [
        typeof(AbilityEffect.ChangeForm), typeof(AbilityEffect.Draw),
        typeof(AbilityEffect.DrawToHandSize),
        typeof(AbilityEffect.GrantControlledCharacters),
        typeof(AbilityEffect.DealEncounterCards), typeof(AbilityEffect.CreateDrones),
        typeof(AbilityEffect.DealEncounterCard),
    ];
    private static readonly HashSet<Type> PlacementTypes =
    [
        typeof(AbilityEffect.DelayedDiscard), typeof(AbilityEffect.DiscardAtRandom),
        typeof(AbilityEffect.PlaceAtRandom), typeof(AbilityEffect.DiscardTop),
        typeof(AbilityEffect.ShuffleInto), typeof(AbilityEffect.PutIntoPlay),
        typeof(AbilityEffect.PlaceCounters), typeof(AbilityEffect.RemoveCounters),
    ];
    private static readonly HashSet<Type> PowerTypes =
    [
        typeof(AbilityEffect.ReduceNextCardCost), typeof(AbilityEffect.Power),
        typeof(AbilityEffect.ThwartGroup), typeof(AbilityEffect.ActivateEnemies),
    ];

    internal static EffectGroup GroupOf(AbilityEffect effect)
    {
        Type type = effect.GetType();
        if (OrderedTypes.Contains(type)) return EffectGroup.Ordered;
        if (BranchingTypes.Contains(type)) return EffectGroup.Branching;
        if (WrappedTypes.Contains(type)) return EffectGroup.Wrapped;
        if (BasicCardTypes.Contains(type)) return EffectGroup.BasicCard;
        if (ThreatAndGrantTypes.Contains(type)) return EffectGroup.ThreatAndGrant;
        if (PlayerTypes.Contains(type)) return EffectGroup.Player;
        if (PlacementTypes.Contains(type)) return EffectGroup.Placement;
        if (PowerTypes.Contains(type)) return EffectGroup.Power;
        return EffectGroup.Other;
    }

    internal static bool BindingCanChange(AbilityEffect? effect)
    {
        if (effect is null) return false;
        Type type = effect.GetType();
        if (OrderedTypes.Contains(type)) return OrderedBindingCanChange(effect);
        if (BranchingTypes.Contains(type)) return BranchingBindingCanChange(effect);
        if (WrappedTypes.Contains(type)) return WrappedBindingCanChange(effect);
        if (BasicCardTypes.Contains(type)) return BasicCardBindingCanChange(effect);
        if (ThreatAndGrantTypes.Contains(type)) return ThreatBindingCanChange(effect);
        if (PlayerTypes.Contains(type)) return PlayerBindingCanChange(effect);
        if (PlacementTypes.Contains(type)) return PlacementBindingCanChange(effect);
        if (PowerTypes.Contains(type)) return PowerBindingCanChange(effect);
        return NoBindingChange(effect);
    }

    private static bool OrderedBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(BindingCanChange),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(BindingCanChange),
        AbilityEffect.Choose choose => choose.Options.Any(BindingCanChange),
        _ => throw new InvalidOperationException("Unknown ordered binding effect"),
    };

    private static bool BranchingBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Conditional conditional => BindingCanChange(conditional.Test)
            || BindingCanChange(conditional.Then) || BindingCanChange(conditional.Else),
        AbilityEffect.Dependent dependent => BindingCanChange(dependent.Effect) || BindingCanChange(dependent.Continuation),
        AbilityEffect.EachTime each => BindingCanChange(each.Effect)
            || BindingCanChange(each.When) || BindingCanChange(each.Then),
        _ => throw new InvalidOperationException("Unknown branching binding effect"),
    };

    private static bool WrappedBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.EachPlayer each => BindingCanChange(each.Effect),
        AbilityEffect.ForEach repeated =>
            BindingCanChange(repeated.Count) || BindingCanChange(repeated.Effect),
        AbilityEffect.ChooseCard choose => BindingCanChange(choose.From) || BindingCanChange(choose.Effect),
        AbilityEffect.AfterActivation after => BindingCanChange(after.Effect),
        AbilityEffect.PayOrEffect payment => BindingCanChange(payment.Otherwise),
        _ => throw new InvalidOperationException("Unknown wrapped binding effect"),
    };

    private static bool BasicCardBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.CardAction action => BindingCanChange(action.Selection),
        AbilityEffect.Heal heal => BindingCanChange(heal.Card) || BindingCanChange(heal.Amount),
        AbilityEffect.Damage damage => BindingCanChange(damage.Cards) || BindingCanChange(damage.Amount),
        AbilityEffect.AttackDamage damage => BindingCanChange(damage.Cards) || BindingCanChange(damage.Amount),
        AbilityEffect.MoveDamage move => BindingCanChange(move.From) || BindingCanChange(move.To) || BindingCanChange(move.Amount),
        AbilityEffect.IndirectDamage damage => BindingCanChange(damage.Among) || BindingCanChange(damage.Amount),
        AbilityEffect.GiveStatus status => BindingCanChange(status.Cards),
        _ => throw new InvalidOperationException("Unknown basic card binding effect"),
    };

    private static bool ThreatBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.PlaceThreat threat => BindingCanChange(threat.Schemes) || BindingCanChange(threat.Amount),
        AbilityEffect.RemoveThreat threat => BindingCanChange(threat.Schemes) || BindingCanChange(threat.Amount)
            || (threat.OverridesCannotFrom is { } source && BindingCanChange(source)),
        AbilityEffect.PreventThreat threat => BindingCanChange(threat.Amount),
        AbilityEffect.PreventDamage damage => BindingCanChange(damage.Amount),
        AbilityEffect.GrantTrait grant => BindingCanChange(grant.Cards),
        AbilityEffect.GrantField grant => BindingCanChange(grant.Cards) || BindingCanChange(grant.Amount),
        AbilityEffect.PreventDamageWhile prevention => BindingCanChange(prevention.Condition),
        _ => throw new InvalidOperationException("Unknown threat binding effect"),
    };

    private static bool PlayerBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ChangeForm form => form.Player == AbilityPlayer.ChosenPlayer,
        AbilityEffect.Draw draw => BindingCanChange(draw.Players),
        AbilityEffect.DrawToHandSize draw => draw.Player == AbilityPlayer.ChosenPlayer,
        AbilityEffect.GrantControlledCharacters grant =>
            grant.Player == AbilityPlayer.ChosenPlayer || BindingCanChange(grant.Amount),
        AbilityEffect.DealEncounterCards deal => BindingCanChange(deal.Players),
        AbilityEffect.CreateDrones create => BindingCanChange(create.Players),
        AbilityEffect.DealEncounterCard deal =>
            BindingCanChange(deal.Card) || deal.Player == AbilityPlayer.ChosenPlayer,
        _ => throw new InvalidOperationException("Unknown player binding effect"),
    };

    private static bool PlacementBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.DelayedDiscard delayed => BindingCanChange(delayed.Card),
        AbilityEffect.DiscardAtRandom discard => BindingCanChange(discard.Players) || BindingCanChange(discard.Count),
        AbilityEffect.PlaceAtRandom place => BindingCanChange(place.Players)
            || BindingCanChange(place.Count) || BindingCanChange(place.Host),
        AbilityEffect.DiscardTop discard => (discard.Players is { } players && BindingCanChange(players)) || BindingCanChange(discard.Count),
        AbilityEffect.ShuffleInto shuffle => BindingCanChange(shuffle.Cards),
        AbilityEffect.Search => false,
        AbilityEffect.PutIntoPlay entering => BindingCanChange(entering.Card),
        AbilityEffect.PlaceCounters counters => BindingCanChange(counters.Card) || BindingCanChange(counters.Count),
        AbilityEffect.RemoveCounters counters => BindingCanChange(counters.Card),
        _ => throw new InvalidOperationException("Unknown placement binding effect"),
    };

    private static bool PowerBindingCanChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ReduceNextCardCost reduction => reduction.Player == AbilityPlayer.ChosenPlayer || BindingCanChange(reduction.Amount),
        AbilityEffect.Power power => (power.Target is { } target && BindingCanChange(target)) || BindingCanChange(power.Effect),
        AbilityEffect.ThwartGroup thwart => BindingCanChange(thwart.Schemes) || BindingCanChange(thwart.Thwart),
        AbilityEffect.ActivateEnemies activate => BindingCanChange(activate.Enemies)
            || (activate.Against is { } target && BindingCanChange(target)),
        _ => throw new InvalidOperationException("Unknown power binding effect"),
    };

    private static bool NoBindingChange(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Shuffle or AbilityEffect.Search => false,
        AbilityEffect.GainSurge or AbilityEffect.Fixed or AbilityEffect.Generate or AbilityEffect.DoubleResourceFor
            or AbilityEffect.PreventDamageFrom or AbilityEffect.DelayedStun or AbilityEffect.DiscardUntil
            or AbilityEffect.ChooseTopForHand or AbilityEffect.ChooseDiscardToShuffle
            or AbilityEffect.DiscardHandWithResource or AbilityEffect.RecoverDiscardedByResource => false,
        _ => throw new InvalidOperationException("Unknown compiled effect in player-binding analysis"),
    };

    internal static bool BindingCanChange(AbilityPlayerSelection players) => players switch
    {
        AbilityPlayerSelection.OnePlayer one => one.Player == AbilityPlayer.ChosenPlayer,
        AbilityPlayerSelection.AllPlayers => false,
        _ => throw new InvalidOperationException("Unknown compiled player selection in player-binding analysis"),
    };

    private static bool BindingCanChange(AbilityCondition condition) =>
        AbilityBindingAnalysis.BindingCanChange(condition);

    private static bool BindingCanChange(AbilityNumber number) =>
        AbilityBindingAnalysis.BindingCanChange(number);

    private static bool BindingCanChange(AbilityCardSelection selection) =>
        AbilityBindingAnalysis.BindingCanChange(selection);
}
