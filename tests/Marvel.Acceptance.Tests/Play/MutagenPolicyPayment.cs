using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Acceptance.Tests.Play;

internal static class MutagenPolicyPayment
{
    internal static Dictionary<string, long> Values(Affordance option) =>
        option.CostOptions
            .SelectMany(cost => cost.VariableRequests)
            .ToDictionary(
                variable => variable.Name,
                variable => variable.Min,
                StringComparer.Ordinal);

    internal static IReadOnlyList<ResourceAllocation> Allocations(
        Affordance option,
        IReadOnlyList<int> payment,
        IReadOnlyDictionary<string, long> values) =>
        option.CostOptions
            .Select(cost => ResourcePayment.Allocate(cost, payment, values))
            .FirstOrDefault(allocation => allocation is not null)
        ?? [];

    internal static IReadOnlyList<int>? Payment(Affordance option, Game game)
    {
        if (option.CostOptions.Count == 0)
        {
            return [];
        }

        var price = option.CostOptions[0];
        price = price with { Sources = price.Generators.OrderBy(source => KeepValue(game, source) + (price.Cost == "1" && source.Generates.Length > 1 ? 500 : 0)).ToArray() };
        var values = Values(option);

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

    internal static int KeepValue(Game game, ResourceSource source)
    {
        var card = game.State.Seats[game.Pending!.Player].Hand.Cards
            .FirstOrDefault(candidate => candidate.ObjectId == source.Effect);
        if (card is null) return -100;
        return game.State.Facts.Title(card.FaceId) switch
        {
            "Energy" or "Genius" or "Strength" => -50,
            "The Power of Leadership" => -30,
            "Heroic Strike" => 300,
            "Captain America's Shield" => 1000,
            "Super-Soldier Serum" => 200,
            "Shield Block" => 90,
            "Nick Fury" => 80,
            _ => 20,
        };
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
