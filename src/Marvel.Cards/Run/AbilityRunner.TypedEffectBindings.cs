using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    // rr:ability.8.1: only the attached player card's controller can trigger
    // an attachment ability that "uses the word “you” or “your”". The engine
    // represents that text with explicit bindings; literal names and display
    // descriptions are not bindings and cannot change permission.
    private static bool ContainsYouOrYour(AbilityEffect? effect) => effect switch
    {
        null => false,
        AbilityEffect.Sequence sequence => sequence.Effects.Any(ContainsYouOrYour),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(ContainsYouOrYour),
        AbilityEffect.Conditional conditional => ContainsYouOrYour(conditional.Test)
            || ContainsYouOrYour(conditional.Then) || ContainsYouOrYour(conditional.Else),
        AbilityEffect.Dependent dependent => ContainsYouOrYour(dependent.Effect) || ContainsYouOrYour(dependent.Continuation),
        AbilityEffect.EachPlayer each => ContainsYouOrYour(each.Effect),
        AbilityEffect.ForEach repeated => ContainsYouOrYour(repeated.Count) || ContainsYouOrYour(repeated.Effect),
        AbilityEffect.EachTime each => ContainsYouOrYour(each.Effect)
            || ContainsYouOrYour(each.When) || ContainsYouOrYour(each.Then),
        AbilityEffect.Choose choose => choose.Options.Any(ContainsYouOrYour),
        AbilityEffect.ChooseCard choose => ContainsYouOrYour(choose.From) || ContainsYouOrYour(choose.Effect),
        AbilityEffect.AfterActivation after => ContainsYouOrYour(after.Effect),
        AbilityEffect.CardAction action => ContainsYouOrYour(action.Selection),
        AbilityEffect.Heal heal => ContainsYouOrYour(heal.Card) || ContainsYouOrYour(heal.Amount),
        AbilityEffect.Damage damage => ContainsYouOrYour(damage.Cards) || ContainsYouOrYour(damage.Amount),
        AbilityEffect.AttackDamage damage => ContainsYouOrYour(damage.Cards) || ContainsYouOrYour(damage.Amount),
        AbilityEffect.MoveDamage move => ContainsYouOrYour(move.From) || ContainsYouOrYour(move.To) || ContainsYouOrYour(move.Amount),
        AbilityEffect.IndirectDamage damage => ContainsYouOrYour(damage.Among) || ContainsYouOrYour(damage.Amount),
        AbilityEffect.GiveStatus status => ContainsYouOrYour(status.Cards),
        AbilityEffect.ChangeForm form => form.Player == AbilityPlayer.You,
        AbilityEffect.Draw draw => ContainsYouOrYour(draw.Players),
        AbilityEffect.DrawToHandSize draw => draw.Player == AbilityPlayer.You,
        AbilityEffect.PlaceThreat threat => ContainsYouOrYour(threat.Schemes) || ContainsYouOrYour(threat.Amount),
        AbilityEffect.RemoveThreat threat => ContainsYouOrYour(threat.Schemes) || ContainsYouOrYour(threat.Amount)
            || (threat.OverridesCannotFrom is { } source && ContainsYouOrYour(source)),
        AbilityEffect.PreventThreat threat => ContainsYouOrYour(threat.Amount),
        AbilityEffect.PreventDamage damage => ContainsYouOrYour(damage.Amount),
        AbilityEffect.Shuffle shuffle => shuffle.Area == AbilitySearchArea.YourDeck,
        AbilityEffect.PayOrEffect payment => ContainsYouOrYour(payment.Otherwise),
        AbilityEffect.GrantTrait grant => ContainsYouOrYour(grant.Cards),
        AbilityEffect.GrantField grant => ContainsYouOrYour(grant.Cards) || ContainsYouOrYour(grant.Amount),
        AbilityEffect.GrantControlledCharacters grant => grant.Player == AbilityPlayer.You || ContainsYouOrYour(grant.Amount),
        AbilityEffect.PreventDamageWhile prevention => ContainsYouOrYour(prevention.Condition),
        AbilityEffect.DelayedDiscard delayed => ContainsYouOrYour(delayed.Card),
        AbilityEffect.DealEncounterCards deal => ContainsYouOrYour(deal.Players),
        AbilityEffect.CreateDrones create => ContainsYouOrYour(create.Players),
        AbilityEffect.DealEncounterCard deal => ContainsYouOrYour(deal.Card) || deal.Player == AbilityPlayer.You,
        AbilityEffect.DiscardAtRandom discard => ContainsYouOrYour(discard.Players) || ContainsYouOrYour(discard.Count),
        AbilityEffect.PlaceAtRandom place => ContainsYouOrYour(place.Players)
            || ContainsYouOrYour(place.Count) || ContainsYouOrYour(place.Host),
        AbilityEffect.DiscardTop discard => discard.From == AbilitySearchArea.YourDeck
            || (discard.Players is { } players && ContainsYouOrYour(players)) || ContainsYouOrYour(discard.Count),
        AbilityEffect.ShuffleInto shuffle => ContainsYouOrYour(shuffle.Cards) || shuffle.Deck == AbilitySearchArea.YourDeck,
        AbilityEffect.Search search => search.Areas.Contains(AbilitySearchArea.YourDeck),
        AbilityEffect.PutIntoPlay entering => !entering.PrintedDestination || ContainsYouOrYour(entering.Card),
        AbilityEffect.PlaceCounters counters => ContainsYouOrYour(counters.Card) || ContainsYouOrYour(counters.Count),
        AbilityEffect.RemoveCounters counters => ContainsYouOrYour(counters.Card),
        AbilityEffect.ReduceNextCardCost reduction => reduction.Player == AbilityPlayer.You || ContainsYouOrYour(reduction.Amount),
        AbilityEffect.Power power => (power.Target is { } target && ContainsYouOrYour(target)) || ContainsYouOrYour(power.Effect),
        AbilityEffect.ThwartGroup thwart => ContainsYouOrYour(thwart.Schemes) || ContainsYouOrYour(thwart.Thwart),
        AbilityEffect.ActivateEnemies activate => ContainsYouOrYour(activate.Enemies)
            || (activate.Against is { } target && ContainsYouOrYour(target)),
        AbilityEffect.GainSurge or AbilityEffect.Fixed or AbilityEffect.Generate or AbilityEffect.DoubleResourceFor
            or AbilityEffect.PreventDamageFrom or AbilityEffect.DelayedStun or AbilityEffect.DiscardUntil
            or AbilityEffect.ChooseTopForHand or AbilityEffect.ChooseDiscardToShuffle
            or AbilityEffect.DiscardHandWithResource or AbilityEffect.RecoverDiscardedByResource => false,
        _ => throw new InvalidOperationException("Unknown compiled effect in player-binding analysis"),
    };

    private static bool ContainsYouOrYour(AbilityPlayerSelection players) => players switch
    {
        AbilityPlayerSelection.OnePlayer one => one.Player == AbilityPlayer.You,
        AbilityPlayerSelection.AllPlayers => false,
        _ => throw new InvalidOperationException("Unknown compiled player selection in player-binding analysis"),
    };
}
