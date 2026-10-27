using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static class AbilityCostReductionDescription
{
    internal static string? Summary(AbilityEffect effect) => effect is
        AbilityPricingEffect.ReduceNextCardCost { AnyPlayer: true, Amount: AbilityNumber.Constant amount } reduction
        ? $"Reduce the cost of the next {MatchingCard(reduction)} played by any player this phase by {amount.Value}."
        : null;

    private static string MatchingCard(AbilityPricingEffect.ReduceNextCardCost reduction)
    {
        string kind = reduction.Filter?.Kind?.ToString().ToLowerInvariant() ?? "card";
        return reduction.Filter?.Trait is { } trait ? $"{trait} {kind}" : kind;
    }
}
