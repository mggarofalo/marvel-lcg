using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class GameAreaColumnLayoutTests
{
    [Fact]
    public void CardsStartBelowTheOneLineHeaderAndTheBadgeMargin()
    {
        var layout = GameAreaColumnLayout.Arrange(300, 24, 0);
        Assert.Equal(GameAreaColumnLayout.Padding + 24 + GameAreaColumnLayout.BadgeMargin, layout.CardsTop);
        Assert.Equal(300 - layout.CardsTop - GameAreaColumnLayout.Padding, layout.CardsHeight);
    }

    [Fact]
    public void TheAttachmentStripIsReservedBelowTheCards() =>
        Assert.Equal(GameAreaColumnLayout.Arrange(300, 24, 0).CardsHeight - 50, GameAreaColumnLayout.Arrange(300, 24, 50).CardsHeight);

    [Fact]
    public void AColumnTooShortForCardsGivesThemNoRoomRatherThanNegativeRoom() =>
        Assert.Equal(0, GameAreaColumnLayout.Arrange(30, 24, 50).CardsHeight);

    [Fact]
    public void TheSharedPilesStandSideBySideUnderTheStage()
    {
        (Rect2 stage, Rect2 discard, Rect2 deck) = GameAreaColumnLayout.Central(new Rect2(10, 100, 260, 300), 8);
        Assert.Equal(discard.Size, deck.Size);
        Assert.Equal(discard.Position.Y, deck.Position.Y);
        Assert.Equal(discard.End.X + 8, deck.Position.X, 3);
        Assert.Equal(400, deck.End.Y, 3);
        Assert.Equal(300 * GameAreaColumnLayout.PileShare, deck.Size.Y, 3);
        Assert.Equal(GameAreaColumnLayout.PileAspect, deck.Size.X / deck.Size.Y, 3);
        Assert.Equal(140 - discard.Position.X, deck.End.X - 140, 3);
        Assert.Equal(new Rect2(10, 100, 260, 400 - deck.Size.Y - 8 - 100), stage);
    }

    [Fact]
    public void ANarrowCentralColumnSizesItsPilesByWidth()
    {
        (Rect2 stage, Rect2 discard, Rect2 deck) = GameAreaColumnLayout.Central(new Rect2(0, 0, 108, 400), 8);
        Assert.Equal(50, discard.Size.X, 3);
        Assert.Equal(108, deck.End.X - discard.Position.X, 3);
        Assert.True(stage.End.Y <= discard.Position.Y);
    }
}
