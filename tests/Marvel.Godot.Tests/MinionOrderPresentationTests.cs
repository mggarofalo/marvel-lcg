using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class MinionOrderPresentationTests
{
    private static readonly int[] Candidates = [10, 20, 30];
    [Theory]
    [InlineData(PublicDecisionKind.MinionActivationOrder)]
    [InlineData(PublicDecisionKind.SpecialAbilityNext)]
    public void OpeningTheNextChoiceLeavesEveryCardUnselected(PublicDecisionKind kind)
    {
        Prompt prompt = NextPrompt(kind);
        DecisionComposer composer = InitialTableDraft.Create(prompt);
        Assert.True(DecisionCardChoices.IsChoice(prompt));
        Assert.Null(composer.Selected);
        Assert.Empty(composer.Targets);
        Assert.False(composer.TryBuild(out _, out _));
        var operations = new TableDraftOperations(composer);
        Assert.True(operations.TrySelectAffordance(30));
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal(30, decision!.Affordance);
        Assert.Empty(decision.Targets);
        Assert.True(operations.TrySelectAffordance(10));
        Assert.True(composer.TryBuild(out decision, out _));
        Assert.Equal(10, decision!.Affordance);
    }

    [Fact]
    public void SameNamedCardsKeepSeparateIdsAndStaleOrLockedInputCannotSelectAnother()
    {
        DecisionComposer composer = InitialTableDraft.Create(NextPrompt(PublicDecisionKind.MinionActivationOrder));
        int generation = 4;
        long revision = 9;
        bool locked = false;
        var binding = new TableDraftBinding(composer, 4, 9,
            (render, host) => render == generation && host == revision && !locked);
        Assert.True(binding.TrySelectAffordance(30));
        Assert.False(binding.TrySelectAffordance(999));
        locked = true;
        Assert.False(binding.TrySelectAffordance(20));
        locked = false;
        generation++;
        Assert.False(binding.TrySelectAffordance(10));
        generation--;
        revision++;
        Assert.False(binding.TrySelectAffordance(20));
        Assert.Equal(30, composer.Selected!.Id);
    }

    [Theory]
    [InlineData(PublicDecisionKind.Choice, false)]
    [InlineData(PublicDecisionKind.Order, false)]
    [InlineData(PublicDecisionKind.MinionActivationOrder, true)]
    [InlineData(PublicDecisionKind.SpecialAbilityNext, true)]
    public void CardChoicesUseTheEnginePurposeRatherThanLabelText(PublicDecisionKind kind, bool gallery)
    {
        Prompt prompt = NextPrompt(kind);
        Assert.Equal(gallery, DecisionCardChoices.IsChoice(prompt));
        Assert.Null(InitialTableDraft.Create(prompt).Selected);
    }

    [Theory]
    [InlineData("VisibleTargetCard30", "Target30")]
    [InlineData("SearchResult7", "Affordance7")]
    public void CardBodyFocusReturnsToItsSurvivingDraftControl(string face, string key) =>
        Assert.Equal(key, CardChoiceFocus.Key(face));

    [Theory]
    [InlineData("PreviousTargetPage", "NextTargetPage")]
    [InlineData("NextTargetPage", "PreviousTargetPage")]
    [InlineData("PreviousSearchPage", "NextSearchPage")]
    [InlineData("NextSearchPage", "PreviousSearchPage")]
    public void PageBoundariesRetainAnEnabledKeyboardRoute(string page, string alternate)
    {
        Assert.True(CardChoiceFocus.IsPage(page));
        Assert.Equal(alternate, CardChoiceFocus.PairedPage(page));
    }

    private static Prompt NextPrompt(PublicDecisionKind kind) => new(0, Question.Order, TimingPriority.Untimed,
        "fixture", "Choose the next activation", false,
        [.. Candidates.Select(id => new Affordance(id, "Activate_Next", id, 0, "Drone")
            { CommitLabel = "Activate Drone next" })])
    {
        PublicKind = kind,
    };
}
