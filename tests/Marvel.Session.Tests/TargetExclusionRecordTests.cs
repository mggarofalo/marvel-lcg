using System.Text.Json;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class TargetExclusionRecordTests
{
    [Fact]
    public void DurableTargetRecordCapturesAndRoundTripsCombinationExclusions()
    {
        var targets = new TargetRequest([1, 2, 3], 1, 2) { ExclusiveSets = [[1, 2]] };
        TargetRequestRecord record = TargetRequestRecord.From(targets);
        string json = JsonSerializer.Serialize(record, JournalJson.Options);
        TargetRequestRecord restored = JsonSerializer.Deserialize<TargetRequestRecord>(json, JournalJson.Options)!;

        Assert.Equal([1, 2], Assert.Single(restored.ExclusiveSets!));
        Assert.NotEqual(JsonSerializer.Serialize(TargetRequestRecord.From(targets with { ExclusiveSets = null }),
            JournalJson.Options), json);
    }

    [Fact]
    public void ReplayRejectsAnOmittedOrChangedCombinationConstraint()
    {
        var targets = new TargetRequest([1, 2, 3], 1, 2) { ExclusiveSets = [[1, 2]] };
        var prompt = new Prompt(0, Question.Element, TimingPriority.Untimed, "fixture", "Choose", false,
            [new Affordance(9, "Choose", 9, 0, "Choose", targets)]);
        PromptRecord recorded = PromptRecord.From(prompt);
        JournalReplay.RequirePrompt(recorded, prompt, "matching exclusions");
        foreach (IReadOnlyList<IReadOnlyList<int>>? changed in new IReadOnlyList<IReadOnlyList<int>>?[] { null, [[2, 3]] })
        {
            Prompt other = prompt with { Affordances = [prompt.Affordances[0] with
                { Targets = targets with { ExclusiveSets = changed } }] };
            Assert.Throws<ReplayDivergenceException>(() => JournalReplay.RequirePrompt(recorded, other, "changed exclusions"));
        }
    }
}
