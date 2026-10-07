using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class MinionOrderPresentationTests
{
    [Fact]
    public void OpeningTheRequiredOrderingTaskDoesNotChooseOrCommitItsOrder()
    {
        DecisionComposer composer = InitialTableDraft.Create(OrderPrompt());
        Assert.Equal(7, composer.Selected!.Id);
        Assert.Empty(composer.Targets);
        Assert.False(composer.TryBuild(out _, out _));

        composer.AddTarget(30);
        composer.AddTarget(10);
        Assert.False(composer.TryBuild(out _, out _));
        composer.AddTarget(20);
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([30, 10, 20], decision!.Targets);
        Assert.Equal("C → A → B", OrderedCardLabels.Sequence([10, 20, 30], composer.Targets));
        Assert.Equal("3/3 · C → A → B", TableDraftSummary.From(composer, null, compact: true));
    }

    [Fact]
    public void IdenticalCardCopiesKeepIdentityWhileTheirActivationPositionChanges()
    {
        int[] offered = [10, 20, 30];
        DecisionComposer composer = InitialTableDraft.Create(OrderPrompt());
        composer.AddTarget(30);
        composer.AddTarget(10);
        composer.AddTarget(20);
        composer.RemoveTarget(30);
        Assert.Equal("C", OrderedCardLabels.Copy(offered, 30));
        Assert.Equal("+", OrderedCardLabels.Position(composer.Targets, 30));
        Assert.Equal("1", OrderedCardLabels.Position(composer.Targets, 10));
        composer.AddTarget(30);
        Assert.Equal("3", OrderedCardLabels.Position(composer.Targets, 30));
        Assert.Equal("A → B → C", OrderedCardLabels.Sequence(offered, composer.Targets));
    }

    [Theory]
    [InlineData(PublicDecisionKind.Choice, false)]
    [InlineData(PublicDecisionKind.Order, false)]
    [InlineData(PublicDecisionKind.MinionActivationOrder, true)]
    public void CardOrderingUsesItsPublicPurposeRatherThanLabelText(PublicDecisionKind kind, bool open)
    {
        Prompt prompt = OrderPrompt() with { PublicKind = kind };
        Assert.Equal(open, DecisionCardChoices.IsChoice(prompt));
        Assert.Equal(open, InitialTableDraft.Create(prompt).Selected is not null);
    }

    [Fact]
    public void OptionalAndUnavailableOrderingOffersDoNotGetStaged()
    {
        Prompt prompt = OrderPrompt();
        Assert.Null(InitialTableDraft.Create(prompt with { Cancellable = true }).Selected);
        Assert.Null(InitialTableDraft.Create(prompt with
        {
            Affordances = [prompt.Affordances[0] with { Illegal = "Unavailable" }],
        }).Selected);
    }

    [Fact]
    public void CopyLabelsRemainUniqueBeyondTheFirstPageAndAlphabet()
    {
        int[] offered = [.. Enumerable.Range(100, 30)];
        Assert.Equal("Z", OrderedCardLabels.Copy(offered, 125));
        Assert.Equal("AA", OrderedCardLabels.Copy(offered, 126));
        Assert.Equal(30, offered.Select(target => OrderedCardLabels.Copy(offered, target)).Distinct().Count());
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderedCardLabels.Copy(offered, 99));
    }

    [Theory]
    [InlineData("MinionOrderCard30", "Target30")]
    [InlineData("SearchResult7", "Affordance7")]
    public void CardBodyFocusReturnsToItsSurvivingDraftControl(string face, string key) =>
        Assert.Equal(key, CardChoiceFocus.Key(face));

    [Theory]
    [InlineData("PreviousMinionPage", "NextMinionPage")]
    [InlineData("NextMinionPage", "PreviousMinionPage")]
    [InlineData("PreviousSearchPage", "NextSearchPage")]
    [InlineData("NextSearchPage", "PreviousSearchPage")]
    public void PageBoundariesRetainAnEnabledKeyboardRoute(string page, string alternate)
    {
        Assert.True(CardChoiceFocus.IsPage(page));
        Assert.Equal(alternate, CardChoiceFocus.PairedPage(page));
    }

    private static Prompt OrderPrompt() => new(0, Question.Order, TimingPriority.Untimed,
        "fixture", "Choose minion activation order", false,
        [new Affordance(7, "Order", 9, 0, "engaged minions", new TargetRequest([10, 20, 30], 3, 3))])
    {
        PublicKind = PublicDecisionKind.MinionActivationOrder,
    };
}
