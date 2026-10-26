using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>The physical payment order, with taken damage established before resources.</summary>
internal static class AbilityCostOrder
{
    internal static IEnumerable<AbilityCost> Flatten(AbilityCost? cost) => cost switch
    {
        null => [],
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(Flatten),
        _ => [cost],
    };

    internal static bool IsTaking(AbilityCost cost) =>
        cost is AbilityCost.Damage { MustTakeAll: true };
}
