using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class RecipientCommitmentChoicesTests
{
    [Theory]
    [InlineData(7)]
    [InlineData(12)]
    public void NamedRecipientMakesOneCompleteCurrentDraft(int recipient)
    {
        DecisionComposer composer = Composer();
        var binding = new TableDraftBinding(composer, 4, 8, (generation, revision) => generation == 4 && revision == 8);
        Assert.True(RecipientCommitmentChoices.Select(composer, binding, 31, recipient));
        Assert.True(composer.TryBuild(out var answer, out _));
        Assert.Equal(31, answer!.Affordance);
        Assert.Equal([recipient], answer.Targets);
        Assert.Empty(answer.Resources ?? []);
    }

    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 99)]
    public void StaleOrForeignRecipientDoesNotChangeTheDraft(bool current, int recipient)
    {
        DecisionComposer composer = Composer();
        var binding = new TableDraftBinding(composer, 4, 8, (_, _) => current);
        Assert.False(RecipientCommitmentChoices.Select(composer, binding, 31, recipient));
        Assert.Null(composer.Selected);
        Assert.Empty(composer.Targets);
    }

    private static DecisionComposer Composer() => new(new Prompt(0, Question.TurnOption,
        TimingPriority.Untimed, "Turn", "Choose", false,
        [new Affordance(31, "Support", 9, 0, "Support", new TargetRequest([7, 12], 0, 1))
            { DirectRecipientCommit = true }]));
}
