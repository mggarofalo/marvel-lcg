using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionSheetHeadTests
{
    [Fact]
    public void TheHeadStatesWhoAnswersWhetherItIsRequiredAndEachCauseOnce()
    {
        var frame = new DecisionFrame(0, "Thor", DecisionSubject.Inspecting, "False Alarm", false, ["Heimdall"],
            "Choose all remaining cards in top-to-bottom order.");
        Wording[] facts = DecisionSheetHead.Facts(frame, "Pass");
        Assert.Equal(["Thor", "Required", "Heimdall"], facts.Select(fact => fact.Label));
        Assert.Equal("Decision for Thor · Inspecting False Alarm", facts[0].Tooltip);
        Assert.Equal("Choose to continue.", facts[1].Tooltip);
        Assert.DoesNotContain(facts, fact => fact.Label.Contains(frame.Instruction, StringComparison.Ordinal)
            || fact.Tooltip.Contains(frame.Instruction, StringComparison.Ordinal));
    }

    [Fact]
    public void AnOptionalDecisionNamesItsDecline() => Assert.Equal(("Optional", "You may pass this opportunity."),
        DecisionSheetHead.Facts(new DecisionFrame(0, "Thor", DecisionSubject.None, null, true, [], ""), "Pass this opportunity")[1]
            is var fact ? (fact.Label, fact.Tooltip) : default);
}
