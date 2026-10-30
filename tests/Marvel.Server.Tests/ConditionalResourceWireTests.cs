using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ConditionalResourceWireTests
{
    [Fact]
    public void UnconditionalQuoteKeepsTheExistingWireShape()
    {
        var option = new CostOption(3, "1", Sources: [new(7, "G")]);
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Untimed, "test", "test", false,
            [new Affordance(4, "Play", 3, 0, "Play", Costs: [option])]);
        var response = new EngineResponse(EngineProtocol.Version, "r", "g", null, prompt, []);
        string json = System.Text.Encoding.UTF8.GetString(EngineJson.Write(response));
        Assert.DoesNotContain("conditional_bonus", json, StringComparison.Ordinal);
        Assert.DoesNotContain("possible_resources", json, StringComparison.Ordinal);
        Assert.Null(Assert.Single(Assert.Single(EngineJson.Snapshot(response).Prompt!.Affordances[0].CostOptions).Generators).ConditionalBonus);
    }

    [Fact]
    public void ConditionalQuoteSurvivesTheProtocolValueBoundary()
    {
        var source = new ResourceSource(7, "G") { ConditionalBonus = new("G", 3) };
        var option = new CostOption(3, "2", Sources: [source], Components: [new("1"), new("1")]);
        var affordance = new Affordance(4, "Play", 3, 0, "Play", Costs: [option]);
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Untimed, "test", "test", false, [affordance]);
        var response = new EngineResponse(EngineProtocol.Version, "r", "g", null, prompt, []);
        var copied = EngineJson.Snapshot(response);
        var quoted = Assert.Single(Assert.Single(Assert.Single(copied.Prompt!.Affordances).CostOptions).Generators);
        Assert.Equal("G", quoted.Generates);
        Assert.Equal(new ConditionalResourceBonus("G", 3), quoted.ConditionalBonus);
        ResourceAllocation[] payment = [new(7, 0, "G"), new(7, 1, "G")];
        Assert.True(ResourcePayment.Allows(Assert.Single(copied.Prompt.Affordances[0].CostOptions), [7], null, payment));
    }
}
