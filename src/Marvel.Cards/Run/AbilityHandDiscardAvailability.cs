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
        string pool = new([.. generators.SelectMany(source => source.Generates).Order()]);
        if (discards.Length == 0) return Resources.Pays(pool, amount, required);
        if (discards.Length > 1)
            throw new RulesNotImplementedException("multiple hand-discard cost selections require separate commitments");
        int minimum = discardCount ?? AbilityCostSelection.Range(discards[0].Range, candidates.Count).Min;
        var generated = generators.ToDictionary(source => source.Effect, source => source.Generates);
        string[] receipts = [.. candidates.Select(card => generated.GetValueOrDefault(card.ObjectId, ""))];
        return new PaymentSearch(receipts, amount, required).Find(0, minimum, pool);
    }

    private static IEnumerable<AbilityCost> Costs(AbilityCost? cost) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(Costs),
        null => [],
        _ => [cost],
    };

    // Canonical resource pools make equivalent search branches share a failed
    // state. This query owns no world mutation, RNG use or persistent cache.
    private sealed class PaymentSearch(string[] receipts, long amount, string required)
    {
        private readonly HashSet<(int Index, int Remaining, string Pool)> failed = [];

        internal bool Find(int index, int remaining, string pool)
        {
            if (!Resources.Pays(pool, amount, required)) return false;
            if (remaining == 0) return true;
            if (receipts.Length - index < remaining || !failed.Add((index, remaining, pool))) return false;
            return Find(index + 1, remaining - 1, Without(pool, receipts[index]))
                || Find(index + 1, remaining, pool);
        }

        private static string Without(string pool, string receipt)
        {
            foreach (char resource in receipt)
            {
                int index = pool.IndexOf(resource);
                if (index < 0) throw new InvalidOperationException("a hand resource receipt is absent from its pool");
                pool = pool.Remove(index, 1);
            }
            return pool;
        }
    }
}
