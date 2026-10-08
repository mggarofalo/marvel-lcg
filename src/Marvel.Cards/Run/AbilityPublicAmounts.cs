using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Admits numeric descriptions whose evaluation cannot inspect concealed game information.</summary>
internal static class AbilityPublicAmounts
{
    internal static bool IsFixed(AbilityNumber amount) =>
        amount is AbilityNumber.Constant or AbilityNumber.PerPlayer;

    internal static bool IsCurrentStep(AbilityNumber number) => number switch
    {
        AbilityNumber.Constant or AbilityNumber.PerPlayer => true,
        AbilityNumber.Conditional choice =>
            IsCurrentStep(choice.Test) && IsCurrentStep(choice.Then) && IsCurrentStep(choice.Else),
        _ => false,
    };
    internal static bool IsCurrentStep(AbilityCondition condition) =>
        condition is AbilityCondition.Flag { Kind: AbilityConditionFact.FinalStep };
}
