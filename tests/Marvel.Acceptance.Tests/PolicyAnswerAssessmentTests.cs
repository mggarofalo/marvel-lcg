using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class PolicyAnswerAssessmentTests
{
    private static readonly Prompt Assign = new(
        0, Question.Element, TimingPriority.Untimed, Steps.DealAttackDamage,
        "assign 3 indirect attack damage", false,
        [new Affordance(7, "Choose", 7, World.Scenario, "indirectDamage",
            new TargetRequest([10, 20], 3, 3, Rule: "rr:indirect-damage.1", AllowRepeated: true,
                MaximumOccurrences: new Dictionary<int, int> { [10] = 5, [20] = 5 }))]);

    [Fact]
    public void AnAnswerTheEngineRejectsIsRecordedAsThePolicysError()
    {
        var shortAllocation = Decision.Take(7, [10, 20], []);

        var error = Assert.Throws<SimulationRunException>(
            () => PolicyAnswerAssessment.Require(Assign, shortAllocation));

        Assert.StartsWith("policy answer rejected: ", error.Message, StringComparison.Ordinal);
        Assert.Equal("policy_error", SimulationHarnessSupport.FailureCategory(error, "policy"));
    }

    [Fact]
    public void AnAnswerTheEngineAcceptsPassesToResolve()
    {
        var repeated = Decision.Take(7, [10, 20, 10], []);

        var error = Record.Exception(() => PolicyAnswerAssessment.Require(Assign, repeated));

        Assert.Null(error);
    }
}
