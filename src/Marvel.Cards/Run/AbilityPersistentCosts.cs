using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Retains the actor and full supported cost rather than turning an action into a free effect.</summary>
internal static class AbilityPersistentCosts
{
    internal static IReadOnlyList<PersistentCost>? Describe(AbilityCost? cost)
    {
        if (cost is null) return [];
        var found = new List<PersistentCost>();
        return Append(cost, found) ? found.ToArray() : null;
    }

    private static bool Append(AbilityCost cost, List<PersistentCost> found)
    {
        if (cost is AbilityCost.Sequence sequence) return sequence.Costs.All(step => Append(step, found));
        PersistentCost? described = cost switch
        {
            AbilityCost.Exhaust exhaust => new("Exhaust", Target(exhaust.Card)),
            AbilityCost.Discard discard => new("Discard", Target(discard.Card)),
            AbilityCost.RemoveCounters counters => new("RemoveCounters", Target(counters.Card), counters.Count, Counter: counters.Counter),
            AbilityCost.Spend spend => new("SpendResources", "ActingPlayer", Resources: spend.Resources, PrintedOnly: spend.PrintedOnly),
            _ => null,
        };
        if (described is null) return false;
        found.Add(described);
        return true;
    }

    private static string Target(AbilityCostCard card) => card == AbilityCostCard.Source ? "Source" : "ActingIdentity";
}
