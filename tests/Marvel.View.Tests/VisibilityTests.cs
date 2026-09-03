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
    public void ReadableFaceFactsTravelTogetherAndAConcealedDeckGetsNoneOfThem()
    {
        var board = Board();
        Card villain = board.AreaOf(DeckType.VillainArea).Cards[0];
        villain.TakeDamage(2);
        villain.PlaceTokens("c_test", 3);
        board.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Traits.Granted + "AERIAL",
            Affects: villain.ObjectId));
        ViewScope scope = new RestrictedVisibilityPolicy(0).Authorize(null, board.Players);

        WorldDescriptor visible = WorldProjection.For(board, null, [], scope).World;
        CardFaceDescriptor face = Assert.IsType<CardFaceDescriptor>(
            Card(visible, villain.ObjectId).Face);
        CardDescriptor hidden = visible.Areas
            .Single(area => area.Zone == nameof(DeckType.EncounterDeck)).Cards[0];

        Assert.Equal(["BRUTE", "AERIAL"], face.Traits);
        Assert.Equal("4", face.PrintedStats["SCH"]);
        Assert.Equal("Encounter", face.PrintedStats["Class"]);
        Assert.Null(face.Cost);
        Assert.Equal(["Guard"], face.Keywords);
        Assert.Equal("Guard.", face.RulesText);
        Assert.Equal(2, face.Damage);
        Assert.Equal(3, face.Counters["test"]);
        Assert.Null(hidden.Face);
    }

    [Fact]
    public void AnAllyInPlayProjectsItsRemainingHealth()
    {
        var board = Board();
        Card ally = Assert.Single(board.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(1)).Cards);
        ally.TakeDamage(1);

        CardFaceDescriptor face = Assert.IsType<CardFaceDescriptor>(
            Card(WorldProjection.For(
                board, null, [], new PermissiveVisibilityPolicy().Authorize(null, board.Players)).World,
                ally.ObjectId).Face);

        Assert.Equal(2, face.Fields["health"]);
    }

    [Fact]
    public void EngineRuleInsertIsNotAVisibleGameComponent()
    {
        var board = Board();
        Card insert = board.CreateCard("rule-insert", board.AreaOf(DeckType.RemovedArea));

        WorldDescriptor visible = WorldProjection.For(
            board, null, [], new PermissiveVisibilityPolicy().Authorize(null, board.Players)).World;

        Assert.DoesNotContain(
            visible.Areas.SelectMany(area => area.Cards.Concat(area.Removed)),
            card => card.Id == insert.ObjectId);
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

    private static CardDescriptor Card(WorldDescriptor world, string faceId) =>
        Assert.Single(
            world.Areas.SelectMany(area => area.Cards.Concat(area.Removed)),
            card => card.Face?.Id == faceId);

    private static World Board()
    {
        var world = new World(new Facts(), players: 2, seed: 7);
        Seat zero = world.CreateSeat("Zero");
        Seat one = world.CreateSeat("One");
        world.CreateCard("zero-deck", zero.Deck);
        world.CreateCard("one-deck", one.Deck);
        world.CreateCard("zero-hand", zero.Hand);
        world.CreateCard("one-hand", one.Hand);
        Area allies = world.CreateArea(
            DeckType.AlliesArea, cardOwner: 1, playArea: PlayArea.Of(1));
        world.CreateCard("player-ally", allies);
        Area encounter = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("secret-a", encounter);
        world.CreateCard("secret-b", encounter);
        world.CreateCard("public-villain", world.AreaOf(DeckType.VillainArea));
        return world;
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "public-villain" => CardKind.EncounterVillain,
            "player-ally" => CardKind.Ally,
            "rule-insert" => CardKind.Insert,
            _ => CardKind.Event,
        };

        public IReadOnlyList<string> Traits(string faceId) => faceId == "public-villain"
            ? ["BRUTE"]
            : [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            faceId switch
            {
                "public-villain" => new Dictionary<string, string>(StringComparer.Ordinal)
                    { ["SCH"] = "4", ["Class"] = "Encounter" },
                "player-ally" => new Dictionary<string, string>(StringComparer.Ordinal)
                    { ["HP"] = "3" },
                _ => new Dictionary<string, string>(StringComparer.Ordinal),
            };

        public IReadOnlyList<string> Keywords(string faceId) => faceId == "public-villain"
            ? ["Guard"]
            : [];

        public string Text(string faceId) => faceId == "public-villain" ? "Guard." : string.Empty;

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
                && long.TryParse(value, out long parsed) ? parsed : fallback;
    }
}
