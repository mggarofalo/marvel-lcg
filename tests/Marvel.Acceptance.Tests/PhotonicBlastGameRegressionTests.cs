using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class PhotonicBlastGameRegressionTests : SimulationHarnessTestBase
{
    [Theory]
    [InlineData(11u, 277u, 40, "04058")]
    [InlineData(1u, 267u, 14, "01182")]
    public void LethalPhotonicBlastCompletesALegalGameAndReplays(
        uint seed, uint policySeed, int targetStep, string targetFace)
    {
        var config = new SimulationConfig(
            "crossbones", "standard", ["captain_marvel"], null,
            1, [seed], null, null, policySeed, 600, null, RepositoryRoot());
        var records = RoundTrip(config);
        var decision = Assert.Single(records, record =>
            record.GetProperty("type").GetString() == "step"
            && record.GetProperty("step").GetInt32() == targetStep);
        Assert.Equal("01013: choose a card",
            decision.GetProperty("prompt").GetProperty("label").GetString());
        Assert.Equal(targetFace,
            decision.GetProperty("decision").GetProperty("label").GetString());
        Assert.Contains(records, record =>
            record.GetProperty("type").GetString() == "step"
            && record.GetProperty("step").GetInt32() > targetStep);
        Assert.DoesNotContain(records, record =>
            record.GetProperty("type").GetString() == "failure");
    }
}
