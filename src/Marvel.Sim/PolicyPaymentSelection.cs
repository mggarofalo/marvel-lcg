using Marvel.Rules.Prompts;

namespace Marvel.Sim;

/// <summary>Chooses the earliest smallest generator set accepted by engine payment assessment.</summary>
internal static class PolicyPaymentSelection
{
    internal static IReadOnlyList<int>? Payment(Affordance option)
    {
        if (option.CostOptions.Count == 0)
        {
            return [];
        }

        var price = option.CostOptions[0];
        var values = option.CostOptions.SelectMany(price => price.VariableRequests)
            .ToDictionary(variable => variable.Name, variable => variable.Min, StringComparer.Ordinal);

        for (int count = 0; count <= price.Generators.Count; count++)
        {
            var chosen = new List<ResourceSource>(count);
            if (Choose(price, values, count, 0, chosen) is { } payment)
            {
                return payment;
            }
        }

        return null;
    }

    private static IReadOnlyList<int>? Choose(
        CostOption cost,
        IReadOnlyDictionary<string, long> values,
        int remaining,
        int start,
        List<ResourceSource> chosen)
    {
        if (remaining == 0)
        {
            int[] payment = [.. chosen.Select(source => source.Effect)];
            return ResourcePayment.Allocate(cost, payment, values) is not null ? payment : null;
        }

        for (int index = start; index <= cost.Generators.Count - remaining; index++)
        {
            chosen.Add(cost.Generators[index]);
            var payment = Choose(
                cost, values, remaining - 1, index + 1, chosen);
            chosen.RemoveAt(chosen.Count - 1);
            if (payment is not null)
            {
                return payment;
            }
        }

        return null;
    }
}
