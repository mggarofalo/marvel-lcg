using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Decisions.Tests;

public sealed class WindowChoiceCompositionTests
{
    [Fact]
    public void ExactOfferedTupleSelectsOnceAndUnlistedTupleLeavesDraftUnchanged()
    {
        var source = new Affordance(7, "Response", 7, 0, "Response", new TargetRequest([9, 10], 0, 1))
        {
            WindowChoices = [new WindowChoice(9, "Stun Rhino", null), new WindowChoice(10, "Stun enemy", null)],
        };
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Response, "test", "Response?", true, [source]);
        var composer = new DecisionComposer(prompt);
        Assert.False(composer.SelectWindowChoice(7, 99));
        Assert.Null(composer.Selected);
        Assert.True(composer.SelectWindowChoice(7, 10));
        Assert.Equal([10], composer.Targets);
        Assert.False(composer.SelectWindowChoice(7, null));
        Assert.Equal([10], composer.Targets);
        Assert.True(composer.TryBuild(out var answer, out _));
        Assert.Equal(7, answer!.Affordance);
        Assert.Equal([10], answer.Targets);
        Assert.False(new DecisionComposer(prompt with { Cancellable = false }).SelectWindowChoice(7, 10));
    }
}
