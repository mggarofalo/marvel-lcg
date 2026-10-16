using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Builds explicit payment declarations for single-price test actions.</summary>
internal static class PaidDecision
{
    internal static Decision Take(Affordance action, IReadOnlyList<int> paying)
    {
        var price = Assert.Single(action.CostOptions);
        var allocation = ResourcePayment.Allocate(price, paying);
        Assert.NotNull(allocation);
        return Decision.Take(action.Id, [], paying, new Dictionary<string, long>(), allocation);
    }
}
