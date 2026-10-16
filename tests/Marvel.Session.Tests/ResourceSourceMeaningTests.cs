using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class ResourceSourceMeaningTests
{
    [Fact]
    public void ReplayVerifiesResourceTypesAndTheirMeaning()
    {
        var source = new ResourceSource(8, "G") { AbilityName = "Scientist", CostDescription = "" };
        Prompt asked = Payment(source);
        PromptRecord current = PromptRecord.From(asked);
        Assert.Throws<ReplayDivergenceException>(() => JournalReplay.RequirePrompt(
            current, Payment(source with { Generates = "B" }), "changed resources"));
        Assert.Throws<ReplayDivergenceException>(() => JournalReplay.RequirePrompt(
            current, Payment(source with { AbilityName = "Different ability" }), "changed meaning"));
    }

    private static Prompt Payment(ResourceSource source) => new(0, Question.TurnOption,
        TimingPriority.Untimed, "", "Payment", false,
        [new Affordance(1, "Play", 7, 0, "Play card", Costs: [new CostOption(0, "1", Sources: [source])])]);

    [Fact]
    public void DurableGeneratorKeepsTheOfferedAbilityAndItsCosts()
    {
        var source = new ResourceSource(8, "G")
        {
            AbilityName = "Web-Shooter",
            CostDescription = "Exhaust Web-Shooter; Remove 1 web counter from Web-Shooter",
        };
        ResourceSourceRecord captured = ResourceSourceRecord.From(source);
        var restored = JsonSerializer.Deserialize<ResourceSourceRecord>(JsonSerializer.Serialize(captured));
        Assert.Equal(source.AbilityName, restored.AbilityName);
        Assert.Equal(source.CostDescription, restored.CostDescription);
        Assert.Equal(source.Effect, restored.Effect);
        Assert.Equal(source.Generates, restored.Generates);
    }
}
