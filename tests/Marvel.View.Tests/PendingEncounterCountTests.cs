using Marvel.Rules.State;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PendingEncounterCountTests
{
    [Rule("rr:player-deck.1")]
    [Fact]
    public void RecyclingThePlayerDeckImmediatelyProjectsItsWaitingEncounterCard()
    {
        // "That player immediately deals themself one facedown encounter card
        // from the top of the encounter deck."
        World world = VisibilityFixture.Board();
        Card last = Assert.Single(world.Seats[0].Deck.Cards);
        World.MoveToTop(last, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, world.Players);
        Assert.Equal(0, BoardPresentation.From(WorldProjection.For(world, null, [], scope).World)
            .PendingEncounterCount(0));
        Assert.True(PlayerDeck.Reset(world, 0, []));
        BoardPresentation board = BoardPresentation.From(WorldProjection.For(world, null, [], scope).World);
        Assert.Equal(1, board.PendingEncounterCount(0));
        Assert.Equal(0, board.PendingEncounterCount(1));
    }

    [Fact]
    public void DealtCountsStayLocalToTheirPlayerAndNeverRequireConcealedIdentity()
    {
        World world = VisibilityFixture.Board();
        Area first = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0), cardOwner: 0);
        Area second = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1), cardOwner: 1);
        Card reveal = world.CreateCard("secret-a", first);
        world.CreateCard("secret-b", first).TurnFaceDown();
        world.CreateCard("secret-c", second).TurnFaceDown();
        reveal.TurnFaceDown();
        ViewScope[] scopes =
        [
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players),
            new RestrictedVisibilityPolicy(1).Authorize(null, world.Players),
            new PermissiveVisibilityPolicy().Authorize(null, world.Players),
        ];
        foreach (ViewScope scope in scopes)
        {
            BoardPresentation board = BoardPresentation.From(WorldProjection.For(world, null, [], scope).World);
            Assert.Equal(2, board.PendingEncounterCount(0));
            Assert.Equal(1, board.PendingEncounterCount(1));
            Assert.All(board.Areas.Where(area => area.Zone == "DealtEncounterCardsDeck")
                .SelectMany(area => area.Cards), card =>
                {
                    Assert.True(card.Concealed);
                    Assert.Null(card.TargetId);
                    Assert.Null(card.FaceId);
                });
        }
        World.MoveToTop(reveal, world.AreaOf(DeckType.RevealingArea, PlayArea.Of(0), cardOwner: 0));
        reveal.TurnFaceUp();
        BoardPresentation changed = BoardPresentation.From(WorldProjection.For(world, null, [], scopes[0]).World);
        Assert.Equal(1, changed.PendingEncounterCount(0));
        Assert.Equal(1, changed.PendingEncounterCount(1));
        Assert.Equal(0, changed.PendingEncounterCount(2));
    }
}
