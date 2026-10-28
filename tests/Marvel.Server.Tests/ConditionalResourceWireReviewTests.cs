using System.Text;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ConditionalResourceWireReviewTests
{
    [Fact]
    public void ConditionalBonusWithoutComponentMaskIsRejectedAtWireBoundary()
    {
        var source = new ResourceSource(7, "G") { ConditionalBonus = new("G", 1) };
        var option = new CostOption(3, "1", Sources: [source]);
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Untimed, "test", "test", false,
            [new Affordance(4, "Play", 3, 0, "Play", Costs: [option])]);
        var response = new EngineResponse(EngineProtocol.Version, "r", "g", null, prompt, []);
        string valid = Encoding.UTF8.GetString(EngineJson.Write(response));
        Assert.Contains(",\"component_mask\":1", valid, StringComparison.Ordinal);
        string missing = valid.Replace(",\"component_mask\":1", "", StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(Encoding.UTF8.GetBytes(missing)));
    }
}
