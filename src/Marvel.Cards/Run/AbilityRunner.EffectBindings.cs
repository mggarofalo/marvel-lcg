using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static bool BindingCanChange(AbilityEffect? effect) => effect switch
    {
        null => false,
        AbilityEffect.Sequence sequence => sequence.Effects.Any(BindingCanChange),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(BindingCanChange),
        AbilityEffect.Conditional conditional => BindingCanChange(conditional.Test)
            || BindingCanChange(conditional.Then) || BindingCanChange(conditional.Else),
        AbilityEffect.Dependent dependent => BindingCanChange(dependent.Effect) || BindingCanChange(dependent.Continuation),
        AbilityEffect.EachPlayer each => BindingCanChange(each.Effect),
        AbilityEffect.ForEach repeated => BindingCanChange(repeated.Count) || BindingCanChange(repeated.Effect),
        AbilityEffect.EachTime each => BindingCanChange(each.Effect)
            || BindingCanChange(each.When) || BindingCanChange(each.Then),
        AbilityEffect.Choose choose => choose.Options.Any(BindingCanChange),
        AbilityEffect.ChooseCard choose => BindingCanChange(choose.From) || BindingCanChange(choose.Effect),
        AbilityEffect.AfterActivation after => BindingCanChange(after.Effect),
        AbilityEffect.CardAction action => BindingCanChange(action.Selection),
        AbilityEffect.Heal heal => BindingCanChange(heal.Card) || BindingCanChange(heal.Amount),
        AbilityEffect.Damage damage => BindingCanChange(damage.Cards) || BindingCanChange(damage.Amount),
        AbilityEffect.AttackDamage damage => BindingCanChange(damage.Cards) || BindingCanChange(damage.Amount),
        AbilityEffect.MoveDamage move => BindingCanChange(move.From) || BindingCanChange(move.To) || BindingCanChange(move.Amount),
        AbilityEffect.IndirectDamage damage => BindingCanChange(damage.Among) || BindingCanChange(damage.Amount),
        AbilityEffect.GiveStatus status => BindingCanChange(status.Cards),
        AbilityEffect.ChangeForm form => form.Player == AbilityPlayer.ChosenPlayer,
        AbilityEffect.Draw draw => BindingCanChange(draw.Players),
        AbilityEffect.DrawToHandSize draw => draw.Player == AbilityPlayer.ChosenPlayer,
        AbilityEffect.PlaceThreat threat => BindingCanChange(threat.Schemes) || BindingCanChange(threat.Amount),
        AbilityEffect.RemoveThreat threat => BindingCanChange(threat.Schemes) || BindingCanChange(threat.Amount)
            || (threat.OverridesCannotFrom is { } source && BindingCanChange(source)),
        AbilityEffect.PreventThreat threat => BindingCanChange(threat.Amount),
        AbilityEffect.PreventDamage damage => BindingCanChange(damage.Amount),
        AbilityEffect.Shuffle => false,
        AbilityEffect.PayOrEffect payment => BindingCanChange(payment.Otherwise),
        AbilityEffect.GrantTrait grant => BindingCanChange(grant.Cards),
        AbilityEffect.GrantField grant => BindingCanChange(grant.Cards) || BindingCanChange(grant.Amount),
        AbilityEffect.GrantControlledCharacters grant => grant.Player == AbilityPlayer.ChosenPlayer || BindingCanChange(grant.Amount),
        AbilityEffect.PreventDamageWhile prevention => BindingCanChange(prevention.Condition),
        AbilityEffect.DelayedDiscard delayed => BindingCanChange(delayed.Card),
        AbilityEffect.DealEncounterCards deal => BindingCanChange(deal.Players),
        AbilityEffect.CreateDrones create => BindingCanChange(create.Players),
        AbilityEffect.DealEncounterCard deal => BindingCanChange(deal.Card) || deal.Player == AbilityPlayer.ChosenPlayer,
        AbilityEffect.DiscardAtRandom discard => BindingCanChange(discard.Players) || BindingCanChange(discard.Count),
        AbilityEffect.PlaceAtRandom place => BindingCanChange(place.Players)
            || BindingCanChange(place.Count) || BindingCanChange(place.Host),
        AbilityEffect.DiscardTop discard => (discard.Players is { } players && BindingCanChange(players)) || BindingCanChange(discard.Count),
        AbilityEffect.ShuffleInto shuffle => BindingCanChange(shuffle.Cards),
        AbilityEffect.Search => false,
        AbilityEffect.PutIntoPlay entering => BindingCanChange(entering.Card),
        AbilityEffect.PlaceCounters counters => BindingCanChange(counters.Card) || BindingCanChange(counters.Count),
        AbilityEffect.RemoveCounters counters => BindingCanChange(counters.Card),
        AbilityEffect.ReduceNextCardCost reduction => reduction.Player == AbilityPlayer.ChosenPlayer || BindingCanChange(reduction.Amount),
        AbilityEffect.Power power => (power.Target is { } target && BindingCanChange(target)) || BindingCanChange(power.Effect),
        AbilityEffect.ThwartGroup thwart => BindingCanChange(thwart.Schemes) || BindingCanChange(thwart.Thwart),
        AbilityEffect.ActivateEnemies activate => BindingCanChange(activate.Enemies)
            || (activate.Against is { } target && BindingCanChange(target)),
        AbilityEffect.GainSurge or AbilityEffect.Fixed or AbilityEffect.Generate or AbilityEffect.DoubleResourceFor
            or AbilityEffect.PreventDamageFrom or AbilityEffect.DelayedStun or AbilityEffect.DiscardUntil
            or AbilityEffect.ChooseTopForHand or AbilityEffect.ChooseDiscardToShuffle
            or AbilityEffect.DiscardHandWithResource or AbilityEffect.RecoverDiscardedByResource => false,
        _ => throw new InvalidOperationException("Unknown compiled effect in player-binding analysis"),
    };

    private static bool BindingCanChange(AbilityPlayerSelection players) => players switch
    {
        AbilityPlayerSelection.OnePlayer one => one.Player == AbilityPlayer.ChosenPlayer,
        AbilityPlayerSelection.AllPlayers => false,
        _ => throw new InvalidOperationException("Unknown compiled player selection in player-binding analysis"),
    };
}
