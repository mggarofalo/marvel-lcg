using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Allows a fixed draw after an opaque defeat window without predicting its board changes.</summary>
internal static class AbilityDefeatReceiptProjection
{
    internal static bool PreservesReads(AbilityEffect? effect) => effect switch
    {
        null or AbilityEffect.Draw => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(PreservesReads),
        AbilityEffect.Conditional conditional => ReceiptOnly(conditional.Test)
            && PreservesReads(conditional.Then) && PreservesReads(conditional.Else),
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
