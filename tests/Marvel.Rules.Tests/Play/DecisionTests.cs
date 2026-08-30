using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>What one answer to one prompt can say.</summary>
public sealed class DecisionTests
{
    [Rule("rr:initiating-abilities.step.3")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void PayingIsASeparateAnswerFromTargeting()
    {
        // `rr:initiating-abilities` makes them different steps -- 3 determines
        // the cost, 5 pays it, and 2 checked the play restrictions before
        // either. So an answer carries three things and they are not
        // interchangeable.
        var decision = Decision.Take(affordance: 4, targets: [11], paying: [7, 9]);

        Assert.Equal(4, decision.Affordance);
        Assert.Equal([11], decision.Targets);
        Assert.Equal([7, 9], decision.Spent);
    }

    [Fact]
    public void AnAffordanceWithNoCostSpendsNothing()
    {
        Assert.Empty(Decision.Take(4).Spent);
        Assert.Empty(Decision.Decline.Spent);
        Assert.True(Decision.Decline.IsDecline);
    }

    [Fact]
    public void SpendingIsNeverNullEvenWhenResourcesIs()
    {
        // Every caller before this field existed constructs the two-argument
        // form, so the property has to answer for them rather than making them
        // check.
        Assert.Empty(new Decision(1, [2]).Spent);
        Assert.Null(new Decision(1, [2]).Resources);
    }

    [Rule("rr:non-numerical-variable.1")]
    [Fact]
    public void ACostVariableIsDefinedSeparatelyFromItsPayment()
    {
        // X is chosen before modifiers and payment. The generators remain a
        // separate answer, so overpaying cannot silently redefine X.
        var decision = Decision.Take(
            affordance: 4,
            targets: [],
            paying: [7, 9],
            values: new Dictionary<string, long>(StringComparer.Ordinal)
            {
                ["X"] = 3,
            });

        Assert.Equal(3, decision.DefinedValues["X"]);
        Assert.Equal([7, 9], decision.Spent);
    }
}
