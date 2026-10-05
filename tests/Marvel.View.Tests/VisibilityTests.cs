using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;
using static Marvel.View.Tests.VisibilityFixture;

namespace Marvel.View.Tests;

public sealed class VisibilityTests
{
    [Fact]
    public void RestrictedSeatsCannotClaimEachOthersHands()
    {
        var board = Board();
        var seatZero = new RestrictedVisibilityPolicy(0).Authorize(
            new ViewerClaim(Watch: true), board.Players);
        var seatOne = new RestrictedVisibilityPolicy(1).Authorize(
            new ViewerClaim(HotSeat: true), board.Players);

        WorldDescriptor zero = WorldProjection.For(board, null, [], seatZero).World;
        WorldDescriptor one = WorldProjection.For(board, null, [], seatOne).World;

        Assert.NotNull(Hand(zero, 0).Face);
        Assert.Null(Hand(zero, 1).Face);
        Assert.NotNull(Hand(one, 1).Face);
        Assert.Null(Hand(one, 0).Face);
        Assert.Null(Hand(zero, 1).Id);
        Assert.Null(Hand(one, 0).Id);
    }

    [Fact]
    public void ValidViewerClaimsCannotSelectOrNarrowRestrictedAuthority()
    {
        var policy = new RestrictedVisibilityPolicy(0);
        ViewerClaim?[] claims =
        [
            null,
            new ViewerClaim(),
            new ViewerClaim(Seat: 0),
            new ViewerClaim(Seat: 1),
            new ViewerClaim(HotSeat: true),
            new ViewerClaim(Watch: true),
        ];

        Assert.All(claims, claim =>
        {
            ViewScope scope = policy.Authorize(claim, players: 2);
            Assert.True(scope.Includes(0));
            Assert.False(scope.Includes(1));
        });
    }

    [Fact]
    public void RestrictedSeatInvitationsReceiveIndependentServerOwnedScopes()
    {
        var policy = new RestrictedVisibilityPolicy(0);

        ViewerClaim?[] claims =
        [
            null,
            new ViewerClaim(),
            new ViewerClaim(Seat: 0),
            new ViewerClaim(Seat: 1),
            new ViewerClaim(Seat: 2),
            new ViewerClaim(Watch: true),
            new ViewerClaim(HotSeat: true),
        ];

        Assert.All(claims, claim =>
        {
            IReadOnlyList<SeatScope> grants = policy.AdditionalScopes(claim, players: 3);
            Assert.Equal([1, 2], grants.Select(grant => grant.Seat));
            Assert.All(grants, grant =>
            {
                Assert.True(grant.Scope.Includes(grant.Seat));
                Assert.All(
                    Enumerable.Range(0, 3).Where(seat => seat != grant.Seat),
                    seat => Assert.False(grant.Scope.Includes(seat)));
            });
        });
    }

    [Fact]
    public void ValidViewerClaimsAllReceiveTheSameCooperativeTableScope()
    {
        var policy = new PermissiveVisibilityPolicy();
        ViewerClaim?[] claims =
        [
            null,
            new ViewerClaim(),
            new ViewerClaim(Seat: 0),
            new ViewerClaim(Seat: 1),
            new ViewerClaim(HotSeat: true),
            new ViewerClaim(Watch: true),
        ];

        Assert.All(claims, claim =>
        {
            ViewScope scope = policy.Authorize(claim, players: 2);
            Assert.True(scope.Includes(0));
            Assert.True(scope.Includes(1));
            Assert.Empty(policy.AdditionalScopes(claim, players: 2));
        });
    }

    [Fact]
    public void CooperativeTableShowsPlayerCardsButKeepsDrawPilesConcealed()
    {
        var board = Board();
        ViewScope scope = new PermissiveVisibilityPolicy().Authorize(
            new ViewerClaim(Seat: 0), board.Players);

        WorldDescriptor visible = WorldProjection.For(board, null, [], scope).World;

        Assert.NotNull(Hand(visible, 0).Face);
        Assert.NotNull(Hand(visible, 1).Face);
        Assert.NotNull(Card(visible, "player-ally").Face);
        Assert.All(
            visible.Areas
                .Where(area => area.Zone == nameof(DeckType.PlayerDeck))
                .SelectMany(area => area.Cards),
            card =>
            {
                Assert.Null(card.Id);
                Assert.Null(card.Face);
            });
        Assert.All(
            visible.Areas.Single(area => area.Zone == nameof(DeckType.EncounterDeck)).Cards,
            card =>
            {
                Assert.Null(card.Id);
                Assert.Null(card.Face);
            });
    }

    [Fact]
    public void EveryRuntimeAreaIsProjectedWithoutAZoneAllowlist()
    {
        var board = Board();
        Area addedLater = board.CreateArea(
            DeckType.AdditionalDeck, World.Scenario, PlayArea.Of(1));
        board.CreateCard("late-secret", addedLater);
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        WorldDescriptor visible = WorldProjection.For(board, null, [], scope).World;
        AreaDescriptor described = Assert.Single(visible.Areas, area => area.Id == addedLater.Id);

        Assert.Equal(nameof(DeckType.AdditionalDeck), described.Zone);
        Assert.Null(Assert.Single(described.Cards).Face);
    }

    [Fact]
    public void HiddenDeckOrderHasNoStableObjectIdentityOnTheWire()
    {
        var board = Board();
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        AreaDescriptor deck = Assert.Single(
            WorldProjection.For(board, null, [], scope).World.Areas,
            area => area.Zone == nameof(DeckType.EncounterDeck));

        Assert.Equal(2, deck.Cards.Count);
        Assert.All(deck.Cards, card =>
        {
            Assert.Null(card.Id);
            Assert.Null(card.Face);
        });
    }

    [Fact]
    public void EventsCarryOnlyCardsVisibleAfterTheDecision()
    {
        var board = Board();
        Card hidden = board.AreaOf(DeckType.EncounterDeck).Cards[0];
        Card visible = board.AreaOf(DeckType.VillainArea).Cards[0];
        var area = AreaRef.Scenario(nameof(DeckType.EncounterDeck));
        GameEvent[] happened =
        [
            new CardsCreated(
                area,
                [new CreatedCard(hidden.ObjectId, hidden.FaceId),
                 new CreatedCard(visible.ObjectId, visible.FaceId)]),
            new FieldSet(hidden.ObjectId, "health", 3, 2),
            new FieldSet(visible.ObjectId, "health", 3, 2),
            new PlayAreaJoined(0, 0),
            new PlayAreaDetached(1, 0),
        ];
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        IReadOnlyList<GameEvent> events =
            WorldProjection.For(board, null, happened, scope).Events;

        var created = Assert.IsType<CardsCreated>(events[0]);
        Assert.Equal(visible.ObjectId, Assert.Single(created.Cards).Id);
        Assert.Equal(visible.ObjectId, Assert.IsType<FieldSet>(events[1]).Card);
        Assert.IsType<PlayAreaJoined>(events[2]);
        Assert.IsType<PlayAreaDetached>(events[3]);
        Assert.Equal(4, events.Count);
    }

    [Fact]
    public void SearchResultsAreVisibleOnlyToThePlayerBeingAsked()
    {
        var board = Board();
        Card searched = board.AreaOf(DeckType.EncounterDeck).Cards[0];
        var prompt = new Prompt(
            1,
            Question.Element,
            TimingPriority.Untimed,
            "Search",
            "choose one",
            false,
            [new Affordance(
                1, "Choose", 0, 1, "choose",
                new TargetRequest([searched.ObjectId], 1, 1, IsSearch: true))
                { DisplayLabel = "Authorized search result", CommitLabel = "Choose authorized search result" }]);

        VisibleResult forOne = WorldProjection.For(
            board,
            prompt,
            [],
            new RestrictedVisibilityPolicy(1).Authorize(null, board.Players));
        VisibleResult forZero = WorldProjection.For(
            board,
            prompt,
            [],
            new RestrictedVisibilityPolicy(0).Authorize(null, board.Players));

        Assert.NotNull(forOne.Prompt);
        Assert.Equal("Authorized search result", Assert.Single(forOne.Prompt.Affordances).DisplayLabel);
        Assert.Equal("Choose authorized search result", Assert.Single(forOne.Prompt.Affordances).CommitLabel);
        Assert.NotNull(Card(forOne.World, searched.ObjectId).Face);
        Assert.Null(forZero.Prompt);
        Assert.DoesNotContain(
            forZero.World.Areas.SelectMany(area => area.Cards),
            card => card.Id == searched.ObjectId);
    }

    [Fact]
    public void ConcealedCardsOfferedAsIndividualChoicesAreReadableToTheAskedPlayer()
    {
        var board = Board();
        Card lookedAt = board.AreaOf(DeckType.EncounterDeck).Cards[0];
        var prompt = new Prompt(
            1,
            Question.Element,
            TimingPriority.Untimed,
            "Choose",
            "choose one",
            false,
            [new Affordance(
                lookedAt.ObjectId,
                "Choose",
                lookedAt.ObjectId,
                1,
                lookedAt.FaceId)])
        {
            ExposesConcealedCandidates = true,
        };

        VisibleResult visible = WorldProjection.For(
            board,
            prompt,
            [],
            new RestrictedVisibilityPolicy(1).Authorize(null, board.Players));

        CardDescriptor offered = Card(visible.World, lookedAt.ObjectId);
        Assert.Equal(lookedAt.FaceId, offered.Face?.Id);
    }

}
