using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class PolicyPaymentSelectionTests
{
    [Fact]
    public void ResearchPolicySkipsEarlierUnrelatedIconsForEveryTypedVariableSlot()
    {
        var cost = new CostOption(0, "X",
            Sources: [new ResourceSource(1, "B"), new ResourceSource(2, "Y"), new ResourceSource(3, "G")],
            Variables: [new VariableRequest("X", 2, 3)],
            Components: [new ResourceCost("X") { RepeatedResource = Resources.Energy }]);
        var offer = new Affordance(7, "Action", 4, 0, "Synthetic variable payment", Costs: [cost]);
        IReadOnlyList<int>? selected = PolicyPaymentSelection.Payment(offer);
        Assert.Equal([2, 3], selected);
        Assert.NotNull(ResourcePayment.Allocate(cost, selected!, new Dictionary<string, long> { ["X"] = 2 }));
    }
}
