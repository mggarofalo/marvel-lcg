using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RecipientCommitmentTransportTests
{
    [Fact]
    public void RecipientCommitmentRetainsExactIdsCostAndEffectThroughTheWire()
    {
        var targets = new TargetRequest([7, 12], 0, 1)
        { Details = new Dictionary<int, string> { [7] = "Exhaust source; Peter draws 1 card.", [12] = "Exhaust source; Carol draws 1 card." } };
        var offer = new Affordance(31, "Support", 9, 0, "Support", targets)
            { DirectRecipientCommit = true, DeferredTargetSelection = true };
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "Turn", "Choose", false, [offer]);
        var response = new EngineResponse(EngineProtocol.Version, "prompt", "game", null, prompt, []);
        var restored = EngineJson.ReadResponse(EngineJson.Write(response)).Prompt!.Affordances[0];
        Assert.True(restored.DirectRecipientCommit);
        Assert.True(restored.DeferredTargetSelection);
        Assert.Equal([7, 12], restored.Targets!.Legal);
        Assert.Equal(targets.Details, restored.Targets.Details);
        Assert.True(restored.Targets.Allows([]));
        Assert.True(restored.Targets.Allows([12]));
        Assert.False(restored.Targets.Allows([7, 12]));
    }
}
