using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class JournalCaptureIsolationTests
{
    [Fact]
    public void CapturedPromptsRetainTargetsCostsAndNestedGroupsAfterSourceMutation()
    {
        var legal = new List<int> { 10, 11 };
        var group = new List<int> { 10 };
        var groups = new List<IReadOnlyList<int>> { group };
        var limits = new Dictionary<int, int> { [10] = 1 };
        var details = new Dictionary<int, string> { [10] = "One damage" };
        var traits = new List<string> { "AVENGER" };
        var requirements = new List<string> { "R" };
        var sources = new List<ResourceSource> { new(4, "R") };
        var variables = new List<VariableRequest> { new("X", 1, 1) };
        var target = new TargetRequest(legal, 1, 1, groups, traits,
            MaximumOccurrences: limits) { Details = details, ExclusiveSets = groups };
        var cost = new CostOption(10, "1", requirements, "1", requirements, sources, variables,
            [new ResourceCost("1", requirements)]);
        PromptRecord captured = PromptRecord.From(new Prompt(0, Question.TurnOption,
            TimingPriority.Untimed, "", "Choose", false, [new Affordance(7, "Use", 10, 0, "Use", target, [cost])]));
        string expected = JsonSerializer.Serialize(captured, JournalJson.Options);

        legal.Clear(); group.Clear(); groups.Clear(); limits.Clear(); details.Clear();
        traits.Clear(); requirements.Clear(); sources.Clear(); variables.Clear();

        Assert.Equal(expected, JsonSerializer.Serialize(captured, JournalJson.Options));
        TargetRequestRecord recorded = captured.Affordances[0].Targets!;
        Assert.Throws<NotSupportedException>(() => ((IList<int>)recorded.Legal)[0] = 99);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)recorded.Groups![0])[0] = 99);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<int, int>)recorded.MaximumOccurrences!)[10] = 99);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)captured.Affordances[0].Costs[0].Rule!)[0] = "Y");
    }

    [Fact]
    public void CapturedAnswersDoNotShareTargetsVariablesOrPaymentBuffers()
    {
        var targets = new List<int> { 10 };
        var resources = new List<int> { 4 };
        var values = new Dictionary<string, long> { ["X"] = 1 };
        var allocations = new List<ResourceAllocation> { new(4, 0, "R") };
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "", "Choose", false,
            [new Affordance(7, "Use", 10, 0, "Use")]);
        DurableDecision captured = DurableDecision.From(0, prompt, new Decision(7, targets, resources, values, allocations));
        string expected = JsonSerializer.Serialize(captured, JournalJson.Options);

        targets.Clear(); resources.Clear(); values.Clear(); allocations.Clear();

        Assert.Equal(expected, JsonSerializer.Serialize(captured, JournalJson.Options));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, long>)captured.Values)["X"] = 2);
        Assert.Throws<NotSupportedException>(() => ((IList<ResourceAllocation>)captured.Allocations).Clear());
    }
}
