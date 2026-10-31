using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes continuations whose suspension does not depend on a projected board.</summary>
internal static class AbilityPowerContinuationDependencies
{
    internal static bool IsIndependent(AbilityEffect? effect) => effect switch
    {
        null => true,
        AbilityEffect.Draw { Players: AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.You } } => true,
        // The bound recipient is retained across stage settlement. Giving its
        // status is immediate; immunity is evaluated on the settled live board.
        AbilityEffect.GiveStatus
        {
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
        } => true,
        // rr:ability.4: "resolve the ability one sentence at a time." Removing threat
        // chooses nothing, so it resolves on the settled live board after any defeat
        // window. Its amount is fixed or a receipt recorded earlier, such as the excess
        // damage dealt (ruling dd353e34); an amount counted from the board stays unsupported.
        AbilityEffect.RemoveThreat { Amount: AbilityNumber.Constant or AbilityNumber.Result } => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(IsIndependent),
        AbilityEffect.Conditional conditional => SupportedCondition(conditional.Test)
            && IsIndependent(conditional.Then) && IsIndependent(conditional.Else),
        _ => false,
    };

    private static bool SupportedCondition(AbilityCondition condition) => condition switch
    {
        // Inspect both branches without evaluating payment here. PaidWith can
        // fall back to a live paid-resource effect; neither its truth nor a
        // later draw's cards are needed to prove these branches cannot suspend.
        AbilityCondition.PaidWithResource => true,
        // A retained recipient may depart or reenter during damage settlement.
        // Its incarnation-bound presence is decided live, after that work ends.
        AbilityCondition.IsInPlay
        {
            Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
        } => true,
        // The value stays a runtime decision. A bound status lookup can be
        // evaluated after settlement without choosing a new card or recipient.
        AbilityCondition.CardText
        {
            Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Property: AbilityCardTextProperty.Status,
        } => true,
        AbilityCondition.All all => all.Operands.All(SupportedCondition),
        AbilityCondition.Any any => any.Operands.All(SupportedCondition),
        AbilityCondition.Negated negated => SupportedCondition(negated.Operand),
        _ => false,
    };
}
