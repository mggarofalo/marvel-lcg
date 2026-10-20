using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class SelectionSourceTransportTests
{
    [Fact]
    public void CurrentSelectionSourceSurvivesWireWithoutTurningCandidateIntoAnActionSource()
    {
        var offer = new Affordance(23, "Choose", 23, 0, "Current candidate")
            { SelectionSourceId = 17 };
        var prompt = new Prompt(0, Question.Element, TimingPriority.Untimed,
            "Choice", "Choose a card", false, [offer]);
        var response = new EngineResponse(EngineProtocol.Version, "choice", "game",
            Capability: null, prompt, Events: []);

        var restored = EngineJson.ReadResponse(EngineJson.Write(response)).Prompt!.Affordances[0];

        Assert.Equal(17, restored.SelectionSourceId);
        Assert.Equal(23, restored.AnchorId);
        Assert.Equal(23, restored.Id);
        Assert.Null(restored.Targets);
    }
}
