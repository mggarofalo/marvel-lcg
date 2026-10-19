using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;
namespace Marvel.Godot.Tests;

public sealed class CardInspectionDraftTests
{
    [Fact]
    public void SingleInspectionStagesAcknowledgementWithoutSubmittingIt()
    {
        var draft = InitialTableDraft.Create(Prompt("Acknowledge", PublicDecisionKind.CardLook));
        Assert.Equal(7, draft.Selected!.Id);
        Assert.True(draft.TryBuild(out var answer, out _));
        Assert.NotNull(answer);
    }

    [Theory]
    [InlineData("Choose", PublicDecisionKind.CardLook)]
    [InlineData("Acknowledge", PublicDecisionKind.CardSearch)]
    public void CardSelectionsStillRequireAnExplicitChoice(string verb, PublicDecisionKind kind) =>
        Assert.Null(InitialTableDraft.Create(Prompt(verb, kind)).Selected);

    private static Prompt Prompt(string verb, PublicDecisionKind kind) =>
        new(0, Question.Element, TimingPriority.Untimed, "peek", "Inspect", false,
            [new Affordance(7, verb, 42, 0, "Finish looking") { CommitLabel = "Done" }])
        { PublicKind = kind };
}
