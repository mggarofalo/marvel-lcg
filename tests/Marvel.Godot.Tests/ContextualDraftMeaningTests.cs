using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ContextualDraftMeaningTests
{
    [Theory]
    [InlineData(PublicDecisionKind.PlayerAction, false)]
    [InlineData(PublicDecisionKind.Choice, true)]
    [InlineData(PublicDecisionKind.Defense, true)]
    [InlineData(PublicDecisionKind.Response, true)]
    public void OnlyGenericTurnInstructionsYieldToTheSelectedAction(PublicDecisionKind kind, bool remains)
    {
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption,
            TimingPriority.Untimed, "test", "Choose", false,
            [new Affordance(1, "Use", 10, 0, "Helicarrier")]) { PublicKind = kind });

        Assert.True(BoardContextualCopy.ShowsResolution(composer));
        composer.SelectAffordance(1);
        Assert.Equal(remains, BoardContextualCopy.ShowsResolution(composer));
    }

    [Fact]
    public void CompactDraftKeepsTheOfferedCostEffectAndUncertaintyTogether()
    {
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption,
            TimingPriority.Untimed, "test", "Choose", false,
            [new Affordance(1, "Use", 10, 0, "Helicarrier")]));
        composer.SelectAffordance(1);
        var offer = new AffordancePresentation(1, "Helicarrier", "Choose a player for the next-card discount.",
            "Use", "Helicarrier", 10, 0, null, "", [])
        {
            CostDescription = "Exhaust Helicarrier",
            SourceState = "READY",
            Consequence = "The recipient is chosen after activation.",
        };
        var prompt = new PromptPresentation("", "", "", "", "", [offer]);

        string text = Assert.IsType<string>(TableDraftSummary.From(composer, prompt, compact: true));

        Assert.Contains("Exhaust Helicarrier", text);
        Assert.Contains("Choose a player", text);
        Assert.Contains("after activation", text);
        Assert.DoesNotContain("READY", text);
        Assert.DoesNotContain('\n', text);
    }

    [Fact]
    public void CompactPaymentKeepsSelectionAndExcessWithoutRepeatingAllocationInstructions()
    {
        var composer = new DecisionComposer(new Prompt(0, Question.Option,
            TimingPriority.Untimed, "test", "Choose", false,
            [new Affordance(1, "Choose", 10, 0, "spend",
                Costs: [new CostOption(10, "1", Rule: ["Y"],
                    Sources: [new ResourceSource(20, "YY")])])]));
        composer.SelectAffordance(1);
        var prompt = new PromptPresentation("", "", "", "", "",
            [new AffordancePresentation(1, "spend", null, "Choose", "Android Efficiency", 10, 0, null, "", [])]);

        string unpaid = Assert.IsType<string>(TableDraftSummary.From(composer, prompt, compact: true));
        Assert.Equal("0 resources selected.", unpaid);
        Assert.False(composer.TryBuild(out _, out _));

        composer.ToggleResource(20);
        string paid = Assert.IsType<string>(TableDraftSummary.From(composer, prompt, compact: true));
        Assert.Contains("2 resources selected", paid);
        Assert.Contains("1 excess resource will be lost", paid);
        Assert.True(composer.TryBuild(out _, out _));
    }
}
