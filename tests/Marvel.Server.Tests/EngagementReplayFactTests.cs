using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EngagementReplayFactTests
{
    [Theory]
    [InlineData("initial_rng")]
    [InlineData("initial_events")]
    [InlineData("initial_digest")]
    [InlineData("decision_prompt")]
    [InlineData("decision_events")]
    [InlineData("decision_rng")]
    [InlineData("decision_digest")]
    [InlineData("current_prompt")]
    public void V26AdmissionNeverReplacesAnOriginalRecordedFact(string fact)
    {
        AdditiveContentUpgradeTests.WithAddedScenario((root, _) =>
        {
            var (previous, factory) = ReplayContractUpgradeTests.Fixture(root);
            var original = previous.Save;
            var unit = original.Units[0];
            var step = unit.Decisions[0];
            var extraEvent = JournalJson.Event(new Marvel.Rules.Events.PhaseStarted("player"));
            var changed = fact switch
            {
                "initial_rng" => original with { Initial = original.Initial with { RngWords = original.Initial.RngWords + 1 } },
                "initial_events" => original with { Initial = original.Initial with { Events = [.. original.Initial.Events, extraEvent] } },
                "initial_digest" => original with { Initial = original.Initial with { StateDigest = "changed" } },
                "current_prompt" => original with { CurrentPrompt = original.CurrentPrompt! with { Label = "changed" } },
                _ => original with { Units = [unit with { Decisions = [Alter(step, fact, extraEvent), .. unit.Decisions.Skip(1)] }, .. original.Units.Skip(1)] },
            };
            string bytes = SessionSaveJson.Write(changed);
            var route = Assert.IsType<SessionContentUpgrade>(factory.ContentUpgrade(changed.Compatibility));
            Assert.Throws<ReplayDivergenceException>(() => route.Verify(changed,
                setup => ReplayContractUpgradeTests.Open(factory, setup)));
            Assert.Equal(bytes, SessionSaveJson.Write(changed));
        });
    }

    private static JournalStep Alter(JournalStep step, string fact, System.Text.Json.JsonElement extraEvent) => fact switch
    {
        "decision_prompt" => step with { Prompt = step.Prompt with { Label = "changed" } },
        "decision_rng" => step with { RngWords = step.RngWords + 1 },
        "decision_digest" => step with { StateFingerprint = "changed" },
        // An extra synthetic receipt changes the ordered sequence even if its event shape is valid.
        _ => step with { Events = [.. step.Events, extraEvent] },
    };
}
