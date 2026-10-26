using System.Reflection;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

public sealed class ResourceCoverageRegressionTests
{
    [Theory]
    [InlineData("GY")]
    [InlineData("YG")]
    public void EqualCoverageReservesBothTypedComponentsBeforeGeneric(string icons)
    {
        // Synthetic contract. Repository policy prefers typed coverage at equal total coverage.
        var cost = new CostOption(0, "3", Sources: [new(7, icons)], Components:
            [new("1", ["Y"]), new("1", ["Y"], Printed: true), new("1")]);
        var progress = ResourcePaymentProgress.Components(cost, [7])!;
        Assert.Equal([0, 0, 1], progress.Select(component => component.Remaining));
        Assert.Null(ResourcePayment.Allocate(cost, [7]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InterchangeableIconsHaveBoundedWorkWhenTypedPaymentIsShort(bool separateSources)
    {
        // Synthetic shortage: unselected sources cannot improve selected payment progress.
        ResourceSource[] selected = separateSources
            ? [.. Enumerable.Range(0, 9).Select(index => new ResourceSource(index + 10, "Y")), new(30, "BBBB")]
            : [new(7, "YYYYYYYYYBBBB")];
        var cost = new CostOption(0, "12", Sources: [.. selected, new(99, "YY")], Components:
            [new("1"), new ResourceCost("11") { RepeatedResource = 'Y' }]);
        int[] paying = selected.Select(source => source.Effect).ToArray();
        var progress = ResourcePaymentProgress.Components(cost, paying)!;
        Assert.Equal([0, 2], progress.Select(component => component.Remaining));
        Assert.Equal([0, 0], ResourcePaymentProgress.Components(cost, [.. paying, 99])!
            .Select(component => component.Remaining));

        // Each memo state consists of a slot position and the remaining counts of
        // the two interchangeable icon types. This bound excludes icon permutations.
        Assert.InRange(EvaluatedStates(cost, selected), 1, 13 * 10 * 5);
    }

    [Theory]
    [InlineData("GGB", 0, 0)]
    [InlineData("GGG", 0, 1)]
    [InlineData("YYY", 1, 1)]
    public void WildDeclarationsAndDistinctnessRemainLocalToEachComponent(string icons, int distinct, int printed)
    {
        var cost = new CostOption(0, "3", Sources: [new(7, icons)], Components:
            [new ResourceCost("2") { DistinctTypes = true }, new("1", ["B"], Printed: true)]);
        Assert.Equal([distinct, printed], ResourcePaymentProgress.Components(cost, [7])!
            .Select(component => component.Remaining));
        Assert.Equal(distinct + printed == 0, ResourcePayment.Allocate(cost, [7]) is not null);
    }

    [Fact]
    public void SourceBoundDeclarationsCannotBorrowAnEquivalentIconFromAnotherSource()
    {
        var cost = new CostOption(0, "2", Sources: [new(7, "G"), new(8, "Y")], Components:
            [new("1", ["Y"]), new("1", ["Y"], Printed: true)]);
        Assert.True(ResourcePayment.Allows(cost, [7, 8], null, [new(7, 0, "Y"), new(8, 1, "Y")]));
        Assert.False(ResourcePayment.Allows(cost, [7, 8], null, [new(8, 0, "Y"), new(7, 1, "Y")]));
    }

    [Theory]
    [InlineData(7, 8, 0)]
    [InlineData(7, 7, 1)]
    [InlineData(8, 8, 1)]
    public void PartialCoverageKeepsSourceBoundIconsSeparate(int firstSource, int secondSource, int shortage)
    {
        var cost = new CostOption(0, "2", Sources: [new(7, "Y"), new(8, "Y")],
            Components: [new("1", ["Y"]), new("1", ["Y"])]);
        object search = CreateSearch(cost, cost.Generators.ToArray(), [firstSource, secondSource]);
        var uncovered = (System.Collections.IEnumerable)search.GetType()
            .GetMethod("Uncovered", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(search, null)!;
        Assert.Equal(shortage, uncovered.Cast<object>().Count());
    }

    private static int EvaluatedStates(CostOption cost, ResourceSource[] sources)
    {
        // Internal instrumentation measures search work; no wall-clock threshold enters the suite.
        object search = CreateSearch(cost, sources);
        Type searchType = search.GetType();
        searchType.GetMethod("Uncovered", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(search, null);
        return (int)searchType.GetProperty("EvaluatedStates", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(search)!;
    }

    private static object CreateSearch(CostOption cost, ResourceSource[] sources, int[]? boundSources = null)
    {
        Assembly assembly = typeof(ResourcePaymentProgress).Assembly;
        Type requirements = assembly.GetType("Marvel.Rules.Prompts.ResourcePaymentRequirements", true)!;
        var slots = (System.Collections.IList)requirements.GetMethod("Slots", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [cost.ResourceCosts, null])!;
        for (int index = 0; boundSources is not null && index < slots.Count; index++)
        {
            object slot = slots[index]!;
            slot.GetType().GetProperty("Source")!.SetValue(slot, boundSources[index]);
            slots[index] = slot;
        }
        Type searchType = assembly.GetType("Marvel.Rules.Prompts.ResourceCoverageSearch", true)!;
        return Activator.CreateInstance(searchType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, [slots, sources], null)!;
    }
}
