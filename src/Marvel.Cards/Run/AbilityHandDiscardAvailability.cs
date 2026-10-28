using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Finds a hand-discard selection that leaves enough independent event resources.</summary>
internal static class AbilityHandDiscardAvailability
{
    internal static bool CanPay(AbilityCost? cost, IReadOnlyList<Card> candidates,
        IReadOnlyList<ResourceSource> generators, long amount, string required, int? discardCount = null)
    {
        var discards = Costs(cost).OfType<AbilityCost.DiscardFromHand>().ToArray();
        if (discards.Length > 1)
            throw new RulesNotImplementedException("multiple hand-discard cost selections require separate commitments");
        int minimum = discards.Length == 0 ? 0
            : discardCount ?? AbilityCostSelection.Range(discards[0].Range, candidates.Count).Min;
        return new PaymentSearch(candidates, generators, amount, required).Find(0, minimum, []);
    }

    private static IEnumerable<AbilityCost> Costs(AbilityCost? cost) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(Costs),
        null => [],
        _ => [cost],
    };

    private sealed class PaymentSearch(
        IReadOnlyList<Card> candidates, IReadOnlyList<ResourceSource> generators, long amount, string required)
    {
        internal bool Find(int index, int remaining, HashSet<int> discarded)
        {
            var available = generators.Where(source => !discarded.Contains(source.Effect)).ToArray();
            var price = new CostOption(0, amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Rule: [required], Sources: available);
            if (ResourcePayment.Allocate(price, available.Select(source => source.Effect).ToArray()) is null)
                return false;
            if (remaining == 0) return true;
            if (candidates.Count - index < remaining) return false;
            discarded.Add(candidates[index].ObjectId);
            bool found = Find(index + 1, remaining - 1, discarded);
            discarded.Remove(candidates[index].ObjectId);
            return found || Find(index + 1, remaining, discarded);
        }
    }
}
