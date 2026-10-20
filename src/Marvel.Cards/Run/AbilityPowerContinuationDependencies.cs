using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes continuations whose suspension does not depend on a projected board.</summary>
internal static class AbilityPowerContinuationDependencies
{
    internal static bool IsIndependent(AbilityEffect? effect) => effect switch
    {
        null => true,
        AbilityEffect.Draw { Players: AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.You } } => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(IsIndependent),
        AbilityEffect.Conditional conditional => PaymentOnly(conditional.Test)
            && IsIndependent(conditional.Then) && IsIndependent(conditional.Else),
        _ => false,
    };

    private static bool PaymentOnly(AbilityCondition condition) => condition switch
    {
        // Inspect both branches without evaluating payment here. PaidWith can
        // fall back to a live paid-resource effect; neither its truth nor a
        // later draw's cards are needed to prove these branches cannot suspend.
        AbilityCondition.PaidWithResource => true,
        AbilityCondition.All all => all.Operands.All(PaymentOnly),
        AbilityCondition.Any any => any.Operands.All(PaymentOnly),
        AbilityCondition.Negated negated => PaymentOnly(negated.Operand),
        _ => false,
    };
}
