using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class PhotonicBlastGameRegressionTests : SimulationHarnessTestBase
{
    [Theory]
    [InlineData(49u, 315u, 18, "04058")]
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
        int target = decision.GetProperty("decision").GetProperty("anchor_id").GetInt32();
        var events = decision.GetProperty("events").EnumerateArray().ToList();
        AssertLethalTarget(events, target);
        if (targetFace == "04058")
        {
            Assert.Contains(events, item => item.GetProperty("verb").GetString() == "Draw");
            Assert.Contains(events, item => item.GetProperty("kind").GetString() == "CardsMoved"
                && item.GetProperty("from").GetProperty("zone").GetString() == "VillainDeck"
                && item.GetProperty("to").GetProperty("zone").GetString() == "VillainArea");
            // Crossbones II attaches his Machine Gun; it later fires against Captain Marvel.
            Assert.Contains(records, record =>
                record.GetProperty("type").GetString() == "step"
                && record.GetProperty("step").GetInt32() > targetStep
                && record.GetProperty("events").EnumerateArray().Any(item =>
                    item.GetProperty("kind").GetString() == "AbilityWindowResult"
                    && item.TryGetProperty("subject_faces", out var faces)
                    && faces.EnumerateObject().Any(face => face.Value.GetString() == "04064")));
        }
        Assert.Contains(records, record =>
            record.GetProperty("type").GetString() == "step"
            && record.GetProperty("step").GetInt32() > targetStep);
        Assert.DoesNotContain(records, record =>
            record.GetProperty("type").GetString() == "failure");
    }

    private static void AssertLethalTarget(
        IReadOnlyList<System.Text.Json.JsonElement> events, int target)
    {
        Assert.Contains(events, item => item.GetProperty("kind").GetString() == "FieldSet"
            && item.GetProperty("card").GetInt32() == target
            && item.GetProperty("field").GetString() == "health"
            && item.GetProperty("from").GetInt32() > 0
            && item.GetProperty("to").GetInt32() == 0);
        Assert.Contains(events, item => item.GetProperty("kind").GetString() == "CardsMoved"
            && item.GetProperty("verb").GetString() == "Defeat"
            && item.GetProperty("cards").EnumerateArray().Any(card =>
                card.GetProperty("card").GetInt32() == target));
    }
}
