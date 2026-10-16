using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Session;
using Xunit;

namespace Marvel.Decisions.Tests;

public sealed class DecisionAuthorityTests
{
    [Theory]
    [InlineData("X")]
    [InlineData("Y")]
    public void DraftAndReplayAcceptTheSameAlternativeWithoutCombiningVariables(string otherName)
    {
        var option = new Affordance(4, "Use", 10, 0, "Choose a price", Costs:
        [
            new CostOption(0, "0", Variables: [new VariableRequest("X", 1, 1)]),
            new CostOption(0, "0", Variables: [new VariableRequest(otherName, 2, 2)]),
        ]);
        Prompt prompt = Ask(option);
        var composer = new DecisionComposer(prompt);
        composer.SelectAffordance(4);
        composer.SelectCost(1);
        composer.Define(otherName, 2);

        Assert.True(composer.TryBuild(out Decision? decision, out string? error), error);
        Assert.True(DecisionAssessment.Assess(prompt, decision!, actor: 0).IsAccepted);
        DurableDecision recorded = DurableDecision.From(0, prompt, decision!);
        Decision replayed = recorded.Resolve(Ask(option with { Id = 99 }));
        Assert.Equal(99, replayed.Affordance);
        Assert.Equal(decision!.DefinedValues, replayed.DefinedValues);
    }

    [Fact]
    public void ReplayRejectsTheSameWrongTargetPriceAsTheDraft()
    {
        Prompt prompt = Ask(new Affordance(4, "Use", 10, 0, "Target price", new TargetRequest([20, 21], 1, 1),
            [new CostOption(20, "0"), new CostOption(21, "1", Sources: [])]));
        var composer = new DecisionComposer(prompt);
        composer.SelectAffordance(4);
        composer.SelectTargets([21]);
        composer.SelectCost(0);

        Assert.False(composer.TryBuild(out _, out _));
        var forged = new Decision(4, [21]);
        Assert.Throws<ReplayDivergenceException>(() => DurableDecision.From(0, prompt, forged).Resolve(prompt));

        composer.SelectTargets([20]);
        composer.SelectCost(0);
        Assert.True(composer.TryBuild(out Decision? valid, out _));
        Assert.Equal([20], DurableDecision.From(0, prompt, valid!).Resolve(prompt).Targets);
    }

    private static Prompt Ask(params Affordance[] options) =>
        new(0, Question.TurnOption, TimingPriority.Untimed, "", "Choose", false, options);
}
