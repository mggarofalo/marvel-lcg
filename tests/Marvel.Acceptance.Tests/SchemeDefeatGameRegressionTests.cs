using Marvel.Session;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SchemeDefeatGameRegressionTests : SimulationHarnessTestBase
{
    [Theory]
    [InlineData(13, 89, 88, "Thwart")]
    public void RecordedSupportAndAllyDefeatsCompleteWithExactResults(
        int seed, int decisions, int defeatStep, string verb)
    {
        string path = Path.Combine(RepositoryRoot(), "tests", "Marvel.Acceptance.Tests",
            "Fixtures", $"scheme-defeat-{seed}.jsonl.gz");
        string[] lines = [.. SimulationRecordFiles.ReadLines(path)];
        var header = SimulationRecordContract.Read<HeaderRecord>(lines[0], "header");
        var start = SimulationRecordContract.Read<StartRecord>(lines[1], "start");
        Assert.Equal("campaign-research", header.Policy);
        Assert.Equal((uint)seed, start.Seed);
        var config = new SimulationConfig(header.Scenario, header.Difficulty, header.Heroes,
            header.ModularSets, 1, [start.Seed], null, null, header.PolicySeed,
            header.DecisionLimit, null, RepositoryRoot());
        var opened = SimulationHarnessSupport.Open(SimulationData.Load(RepositoryRoot()), config, start.Seed);
        var game = opened.Game;
        Assert.Equal(start.InitialDigest, game.State.Digest().Canonical());
        JournalReplay.RequireEvents(start.SetupEvents, opened.SetupEvents, "setup");

        // These are recorded legal decisions, not a regeneration of the research
        // bot's choices. Production replay comparisons still check every result.
        for (int index = 0; index < decisions; index++)
        {
            var step = SimulationRecordContract.Read<StepRecord>(lines[index + 2], "step");
            Assert.Equal(index, step.Step);
            var prompt = Assert.IsType<Marvel.Rules.Prompts.Prompt>(game.Pending);
            JournalReplay.RequirePrompt(step.Prompt, prompt, $"step {index}");
            if (index == defeatStep) Assert.Equal(verb, step.Decision.Verb);
            var answer = new DurableDecision(
                DurableDecision.SimulationActor(prompt, step.Decision), step.Decision,
                step.Targets, step.Resources, step.Values, step.Allocations).Resolve(prompt);
            var result = game.Resolve(answer);
            JournalReplay.RequireEvents(step.Events, result.Events, $"step {index}");
            JournalReplay.RequireFingerprint(step.Digest, game.State.Digest().Fingerprint(), $"step {index}");
        }
        var terminal = SimulationRecordContract.Read<ResultRecord>(lines[decisions + 2], "result");
        Assert.Equal(decisions, terminal.Decisions);
        Assert.Equal(terminal.Outcome, game.State.Result.ToString());
        Assert.Equal(terminal.TerminalDigest, game.State.Digest().Canonical());
        Assert.Null(game.Pending);
        Assert.Equal(decisions + 4, lines.Length);
    }
}
