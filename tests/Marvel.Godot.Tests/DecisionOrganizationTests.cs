using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionOrganizationTests
{
    [Theory]
    [InlineData("HeroArea", false, 1)]
    [InlineData("AlliesArea", false, 1)]
    [InlineData("UpgradesArea", false, 2)]
    [InlineData("SupportsArea", false, 2)]
    [InlineData("HandsArea", false, 3)]
    [InlineData(null, true, 3)]
    [InlineData(null, false, 0)]
    public void TurnCatalogueUsesAuthorizedSourceLocation(string? zone, bool play, int group)
    {
        var offer = Offer(1) with { PlaysCard = play };
        Assert.Equal(group, CompleteChoiceGroups.Category(offer, true, zone));
        Assert.Equal(0, CompleteChoiceGroups.Category(offer, false, zone));
    }

    [Fact]
    public void IdenticalPhysicalSourcesAreSeparateAndMultipleActionsShareTheirSource()
    {
        var first = Offer(1) with { AnchorKind = Marvel.Rules.Prompts.AffordanceAnchorKind.Card };
        Assert.Equal(CompleteChoiceGroups.SourceKey(first), CompleteChoiceGroups.SourceKey(first with { Id = 2 }));
        Assert.NotEqual(CompleteChoiceGroups.SourceKey(first), CompleteChoiceGroups.SourceKey(first with { AnchorId = 3 }));
    }

    [Fact]
    public void ObligationCauseIsVisibleWithoutMovingItAndAbsentCausesAreNotRemembered()
    {
        var card = new BoardCardPresentation(7, 1, false, "Business Problems", "", "Obligation", "", []);
        var board = new BoardPresentation([new BoardAreaPresentation(10, "Obligations", "", [card], [])
            { Zone = "ObligationsArea" }]);
        var prompt = new PromptPresentation("Choose", "Decision for Iron Man", "Resolve", "Choose", "", [])
            { ContextCards = [card] };
        Assert.Equal(card, Assert.Single(DecisionContextCards.MissingFromTable(prompt, board)));
        Assert.Equal("ObligationsArea", board.Areas[0].Zone);
        Assert.Empty(DecisionContextCards.MissingFromTable(prompt with { ContextCards = [] }, board));
        Assert.Empty(DecisionContextCards.MissingFromTable(null, board));
        Assert.Empty(DecisionContextCards.MissingFromTable(prompt with { ContextCards = [card with { Concealed = true }] }, board));
        Assert.Equal(card, Assert.Single(DecisionContextCards.MissingFromTable(prompt,
            board with { Areas = [board.Areas[0] with { Zone = "UpgradesArea", Seat = 1 }] })));
        Assert.Empty(DecisionContextCards.MissingFromTable(prompt,
            board with { Areas = [board.Areas[0] with { Zone = "RevealingArea" }] }));
    }

    private static AffordancePresentation Offer(int id) => new(id, "Action", null, "Action", "Gauntlets", 8, 0, null, "", []);
}
