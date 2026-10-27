using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes receipt continuations that need no prediction of an opaque defeat window.</summary>
internal static class AbilityDefeatReceiptProjection
{
    internal static bool PreservesReads(AbilityEffect? effect, AbilityPowerState state,
        AbilityAdmission.AbilityAdmissionScope cast) => effect switch
    {
        null or AbilityEffect.Draw => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(child => PreservesReads(child, state, cast)),
        AbilityEffect.Conditional conditional => ReceiptOnly(conditional.Test)
            && PreservesReads(conditional.Then, state, cast) && PreservesReads(conditional.Else, state, cast),
        // rr:target.4.1: an effect "does not resolve against" invalid targets.
        // Engine choice: a receipt-driven removal from the retained chosen copy
        // needs no board projection once that copy is proven to leave play.
        // The live continuation still owns its captured incarnation and receipt.
        AbilityEffect.RemoveThreat
        {
            Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen } chosen,
            Amount: AbilityNumber.Result,
        } => AbilityInitiationPrimitives.Find(chosen, cast) is { } card && state.Discarded.Contains(card.ObjectId),
        _ => false,
    };

    private static bool ReceiptOnly(AbilityCondition condition) => condition switch
    {
        AbilityCondition.AtLeast number => ReceiptOnly(number.Value) && ReceiptOnly(number.Count),
        AbilityCondition.All all => all.Operands.All(ReceiptOnly),
        AbilityCondition.Any any => any.Operands.All(ReceiptOnly),
        AbilityCondition.Negated negated => ReceiptOnly(negated.Operand),
        _ => false,
    };

    private static bool ReceiptOnly(AbilityNumber number) => number is AbilityNumber.Constant or AbilityNumber.Result;
}
