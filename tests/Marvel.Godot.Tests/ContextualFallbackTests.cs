using Marvel.Client;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ContextualFallbackTests
{
    [Fact]
    public void RequiredAlternativesRemainVisibleEvenWhenTheirSourceHasAnActionControl()
    {
        var prompt = new PromptPresentation("", "", "", "", "", [Offer(3, 55), Offer(4, 55),
            Offer(5, 55) with { Illegal = "Unavailable" }]);

        Assert.Equal([3, 4], BoardContextualInteractionControls.FallbackActions(prompt,
            new HashSet<int> { 55 }, PublicDecisionKind.Choice).Select(offer => offer.Id));
        Assert.Empty(BoardContextualInteractionControls.FallbackActions(prompt,
            new HashSet<int> { 55 }, PublicDecisionKind.PlayerAction));
    }

    [Fact]
    public void OptionalResponsesRemainBesidePassEvenWithAnInstalledSourceControl()
    {
        var prompt = new PromptPresentation("", "", "", "", "", [Offer(3, 55)]);
        Assert.Equal(3, Assert.Single(BoardContextualInteractionControls.FallbackActions(prompt,
            new HashSet<int> { 55 }, PublicDecisionKind.Response)).Id);
    }

    [Fact]
    public void EveryLegalOfferWithoutAnInstalledCardControlGetsAPrimaryEntry()
    {
        // Synthetic two-option choice: both source anchors are in overflow.
        AffordancePresentation damage = Offer(3, 55);
        AffordancePresentation threat = Offer(4, 55);
        AffordancePresentation unavailable = Offer(5, 60) with { Illegal = "Unavailable" };
        var prompt = new PromptPresentation("", "", "", "", "", [damage, threat, unavailable]);
        Assert.Equal([3, 4], BoardContextualInteractionControls.FallbackActions(prompt,
            new HashSet<int>()).Select(offer => offer.Id));
        Assert.Empty(BoardContextualInteractionControls.FallbackActions(prompt, new HashSet<int> { 55 }));
    }

    [Fact]
    public void InstallingOneCardControlDoesNotHideOffersForAnotherMissingAnchor()
    {
        var prompt = new PromptPresentation("", "", "", "", "", [Offer(3, 55), Offer(4, 60)]);
        Assert.Equal(4, Assert.Single(BoardContextualInteractionControls.FallbackActions(prompt,
            new HashSet<int> { 55 })).Id);
    }

    [Fact]
    public void AChoiceSheetExplainsSafeRetryAndPreservedSelectionWithoutDismissal()
    {
        string notice = CompleteDecisionSheet.RecoveryCopy(GameProgressPresentation.DecisionNotSent(
            new ClientStartupError("transport_unavailable", "The game service is unavailable.")));
        Assert.Contains("Decision not sent", notice);
        Assert.Contains("selection is preserved", notice);
        Assert.Contains("safe to retry", notice);
        Assert.Empty(CompleteDecisionSheet.RecoveryCopy(GameProgressPresentation.Resolving()));
    }

    [Fact]
    public void RecoveryExplanationRemainsAvailableWhenTheSheetIsClosedAndClearsOnNormalProgress()
    {
        // No native nodes are constructed: this isolates lifecycle state.
        // Actual modal reopening and bounds remain native acceptance obligations.
        var sheets = new CompleteDecisionSheetController(null!);
        sheets.PresentProgress(GameProgressPresentation.DecisionNotSent(
            new ClientStartupError("transport_unavailable", "Unavailable")));
        sheets.Closed();
        Assert.Contains("selection is preserved", sheets.CurrentNotice);
        Assert.Contains("safe to retry", sheets.CurrentNotice);
        sheets.PresentProgress(GameProgressPresentation.Resolving());
        Assert.Empty(sheets.CurrentNotice);
    }

    private static AffordancePresentation Offer(int id, int anchor) => new(
        id, "Option", null, "Choose", "Source", anchor, 0, null, "", [])
    {
        Source = new AffordanceSourceDescriptor(AffordanceAnchorKind.Card, anchor, null, 0),
    };
}
