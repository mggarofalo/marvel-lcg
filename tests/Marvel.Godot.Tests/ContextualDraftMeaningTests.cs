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
    public void CompactDefenseKeepsTheSelectedCharacterStateBesideItsCommitmentAndUncertainty()
    {
        // Synthetic authorized offers verify summary composition, not native collection visibility.
        const string uncertainty = "Boosts and later effects are unresolved.";
        var composer = new DecisionComposer(new Prompt(0, Question.Defender,
            TimingPriority.Untimed, "Attack", "Choose a defender", true,
            [new Affordance(1, "Defend", 10, 0, "Defend"), new Affordance(2, "Defend", 20, 0, "Defend")]));
        var widow = new AffordancePresentation(1, "Defend", $"Exhaust Black Widow. {uncertainty}",
            "Defend", "Black Widow", 10, 0, null, "", [])
        { SourceName = "Black Widow", SourceState = "HP 2/2 · Ready" };
        var shuri = new AffordancePresentation(2, "Defend", $"Exhaust Shuri. This ally takes the attack damage; no basic DEF reduction. {uncertainty}",
            "Defend", "Shuri", 20, 0, null, "", [])
        { SourceName = "Shuri", SourceState = "HP 1/3 · Ready · Tough" };
        var prompt = new PromptPresentation("", "", "", "", "", [widow, shuri]);
        composer.SelectAffordance(2);

        string text = Assert.IsType<string>(TableDraftSummary.From(composer, prompt, compact: true));

        Assert.StartsWith("Shuri · HP 1/3 · Ready · Tough", text);
        Assert.Contains(shuri.Description!, text);
        Assert.DoesNotContain("Black Widow", text);
        Assert.DoesNotContain("defeated", text);
        Assert.DoesNotContain('\n', text);
        Assert.Equal(2, composer.Selected!.Id);
        Assert.Empty(composer.Targets);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal(2, decision!.Affordance);

        composer.SelectAffordance(1);
        string changed = Assert.IsType<string>(TableDraftSummary.From(composer, prompt, compact: true));
        Assert.StartsWith("Black Widow · HP 2/2 · Ready", changed);
        Assert.DoesNotContain("Shuri", changed);
        Assert.DoesNotContain("Tough", changed);
    }

    [Fact]
    public void DefenseWithoutAuthorizedSourceStateKeepsOnlyTheOfferedExplanation()
    {
        var composer = new DecisionComposer(new Prompt(0, Question.Defender,
            TimingPriority.Untimed, "Attack", "Defend", true, [new Affordance(1, "Defend", 10, 0, "Defend")]));
        composer.SelectAffordance(1);
        var offer = new AffordancePresentation(1, "Defend", "Boosts are unresolved.",
            "Defend", "Character", 10, 0, null, "", []);
        var prompt = new PromptPresentation("", "", "", "", "", [offer]);

        Assert.Equal("Boosts are unresolved.", TableDraftSummary.From(composer, prompt, compact: true));
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
