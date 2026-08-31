using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

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
    public void AClientSeatAssertionCannotWidenRestrictedAuthority()
    {
        var policy = new RestrictedVisibilityPolicy(0);

        ViewScope claimingOne = policy.Authorize(new ViewerClaim(Seat: 1), players: 2);
        ViewScope claimingAll = policy.Authorize(new ViewerClaim(Watch: true), players: 2);

        Assert.False(claimingOne.Includes(0));
        Assert.False(claimingOne.Includes(1));
        Assert.True(claimingAll.Includes(0));
        Assert.False(claimingAll.Includes(1));
    }

    [Fact]
    public void RestrictedSeatInvitationsReceiveIndependentServerOwnedScopes()
    {
        var policy = new RestrictedVisibilityPolicy(0);

        IReadOnlyList<SeatScope> grants = policy.AdditionalScopes(
            new ViewerClaim(Seat: 0), players: 3);
        IReadOnlyList<SeatScope> denied = policy.AdditionalScopes(
            new ViewerClaim(Seat: 1), players: 3);

        Assert.Equal([1, 2], grants.Select(grant => grant.Seat));
        Assert.All(grants, grant =>
        {
            Assert.True(grant.Scope.Includes(grant.Seat));
            Assert.False(grant.Scope.Includes(0));
        });
        Assert.Empty(denied);
    }

    [Fact]
    public void CooperativePolicyExplicitlyAllowsAWholeTableView()
    {
        var board = Board();
        ViewScope scope = new PermissiveVisibilityPolicy().Authorize(
            new ViewerClaim(HotSeat: true), board.Players);

        WorldDescriptor visible = WorldProjection.For(board, null, [], scope).World;

        Assert.NotNull(Hand(visible, 0).Face);
        Assert.NotNull(Hand(visible, 1).Face);
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
    public void AFacedownCardInPlayKeepsAHandleButNotItsFace()
    {
        var board = Board();
        Area engaged = board.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(1));
        Card drone = board.CreateCard("underlying-player-card", engaged);
        drone.TurnFaceDown();
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        CardDescriptor visible = Assert.Single(
            Assert.Single(
                WorldProjection.For(board, null, [], scope).World.Areas,
                area => area.Id == engaged.Id).Cards);

        Assert.Equal(drone.ObjectId, visible.Id);
        Assert.Equal(CardBack.Player, visible.Back);
        Assert.Null(visible.Face);
    }

    [Fact]
    public void ConcealedPileCardsDoNotExposeMutableState()
    {
        var board = Board();
        Card card = board.AreaOf(DeckType.VillainArea).Cards[0];
        card.Exhaust();
        World.MoveToTop(card, board.AreaOf(DeckType.EncounterDeck));
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        CardDescriptor hidden = Assert.Single(
            WorldProjection.For(board, null, [], scope).World.Areas
                .Single(area => area.Zone == nameof(DeckType.EncounterDeck)).Cards,
            candidate => candidate.Back == CardBack.Encounter);

        Assert.Null(hidden.Id);
        Assert.Null(hidden.Face);
        Assert.True(hidden.Ready);
        Assert.Equal(-1, hidden.Host);
        Assert.False(hidden.FaceUp);
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
        ];
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        IReadOnlyList<GameEvent> events =
            WorldProjection.For(board, null, happened, scope).Events;

        var created = Assert.IsType<CardsCreated>(events[0]);
        Assert.Equal(visible.ObjectId, Assert.Single(created.Cards).Id);
        Assert.Equal(visible.ObjectId, Assert.IsType<FieldSet>(events[1]).Card);
        Assert.Equal(2, events.Count);
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
                new TargetRequest([searched.ObjectId], 1, 1, IsSearch: true))]);

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
        Assert.NotNull(Card(forOne.World, searched.ObjectId).Face);
        Assert.Null(forZero.Prompt);
        Assert.DoesNotContain(
            forZero.World.Areas.SelectMany(area => area.Cards),
            card => card.Id == searched.ObjectId);
    }

    [Fact]
    public void EveryCardFieldIsEitherRedactedOrExplicitlyPublic()
    {
        string[] publicWhileHidden = ["Back", "FaceUp", "Ready", "Host"];
        string[] redacted = ["Id", "Face"];
        string[] declared = typeof(CardDescriptor).GetProperties()
            .Where(property => property.GetMethod?.IsPublic == true)
            .Select(property => property.Name)
            .Order()
            .ToArray();

        Assert.Equal(
            publicWhileHidden.Concat(redacted).Order().ToArray(),
            declared);
    }

    private static CardDescriptor Hand(WorldDescriptor world, int seat) =>
        Assert.Single(Assert.Single(
            world.Areas,
            area => area.Zone == nameof(DeckType.HandsArea) && area.Owner == seat).Cards);

    private static CardDescriptor Card(WorldDescriptor world, int id) =>
        Assert.Single(
            world.Areas.SelectMany(area => area.Cards.Concat(area.Removed)),
            card => card.Id == id);

    private static World Board()
    {
        var world = new World(new Facts(), players: 2, seed: 7);
        Seat zero = world.CreateSeat("Zero");
        Seat one = world.CreateSeat("One");
        world.CreateCard("zero-hand", zero.Hand);
        world.CreateCard("one-hand", one.Hand);
        Area encounter = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("secret-a", encounter);
        world.CreateCard("secret-b", encounter);
        world.CreateCard("public-villain", world.AreaOf(DeckType.VillainArea));
        return world;
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId == "public-villain"
            ? CardKind.EncounterVillain
            : CardKind.Event;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
