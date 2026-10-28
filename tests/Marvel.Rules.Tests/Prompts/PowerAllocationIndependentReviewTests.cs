using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class PowerAllocationIndependentReviewTests
{
    [Fact]
    public void FourDistinctSourcesStillFindTheForcedConditionalOutput() => Check(4);

    private static void Check(int ordinaryCount)
    {
        // Synthetic sources bound a search case in which every source must pay.
        ResourceSource conditional = new(0, "G") { ConditionalBonus = new("G", 1) };
        ResourceSource[] sources = [conditional, .. Enumerable.Range(1, ordinaryCount)
            .Select(id => new ResourceSource(id, "G"))];
        var price = new CostOption(0, (ordinaryCount + 2).ToString(), Sources: sources);
        int[] selected = sources.Select(source => source.Effect).ToArray();
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.AllocateUnambiguousGeneration(price, selected));
        Assert.True(ResourcePayment.Allows(price, selected, null, allocation));
        Assert.Equal("GG", ResourcePayment.GeneratedBy(conditional, allocation));
    }
}
