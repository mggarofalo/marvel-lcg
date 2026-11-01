using Xunit;

namespace Marvel.Godot.Tests;

// Heights in pixels: a 20-pixel heading, 20-pixel explanation lines.
public sealed class GameAreaColumnLayoutTests
{
    [Fact]
    public void CardsStartBelowTheHeadingTheMarkersAndTheBadgeMargin()
    {
        var layout = GameAreaColumnLayout.Arrange(300, new GameAreaColumnText(20, 18, 0, 0), 0);
        Assert.Equal(4 + 20 + 2 + 18 + GameAreaColumnLayout.BadgeMargin, layout.CardsTop);
        Assert.Equal(300 - layout.CardsTop - 4, layout.CardsHeight);
        Assert.Equal(0, layout.DetailsHeight);
    }

    [Fact]
    public void AnExplanationThatFitsIsShownWholeBelowTheCards()
    {
        var layout = GameAreaColumnLayout.Arrange(300, new GameAreaColumnText(20, 0, 40, 20), 0);
        Assert.Equal(40, layout.DetailsHeight);
        Assert.Equal(258, layout.DetailsTop);
        Assert.Equal(212, layout.CardsHeight);
    }

    [Fact]
    public void ALongExplanationGivesWayToTheCardsAndTheAttachmentStrip()
    {
        var layout = GameAreaColumnLayout.Arrange(200, new GameAreaColumnText(20, 0, 400, 20), 30);
        Assert.Equal(40, layout.DetailsHeight);
        Assert.Equal(82, layout.CardsHeight);
        Assert.True(layout.CardsTop + layout.CardsHeight + 30 <= layout.DetailsTop);
    }

    [Fact]
    public void ACrampedColumnKeepsItsCardsBeforeItsExplanation()
    {
        var layout = GameAreaColumnLayout.Arrange(130, new GameAreaColumnText(20, 0, 400, 20), 0);
        Assert.Equal(0, layout.DetailsHeight);
        Assert.Equal(82, layout.CardsHeight);
    }

    [Fact]
    public void AnExplanationThatDoesNotFitShowsWholeLinesOnly()
    {
        var layout = GameAreaColumnLayout.Arrange(250, new GameAreaColumnText(20, 0, 400, 20), 0);
        Assert.Equal(120, layout.DetailsHeight);
        Assert.Equal(82, layout.CardsHeight);
    }
}
