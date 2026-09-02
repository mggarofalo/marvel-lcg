using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class BoardPresentationTests
{
    [Fact]
    public void EveryDescriptorAreaAndBothContainersArePresentedInOrder()
    {
        WorldDescriptor world = World(
            areas:
            [
                Area(4, "VillainArea", -1,
                    cards: [Readable(9, "Rhino")],
                    removed: [Readable(12, "Discarded Scheme")]),
                Area(8, "AdditionalDeck", 1, host: 9),
            ],
            players: [new PlayerDescriptor(1, "Carol Danvers", Eliminated: false)]);

        BoardPresentation board = BoardPresentation.From(world);

        Assert.Equal([4, 8], board.Areas.Select(area => area.Id));
        Assert.Equal("VILLAIN", board.Areas[0].Title);
        Assert.Equal("Rhino", Assert.Single(board.Areas[0].Cards).Title);
        Assert.Equal("Discarded Scheme", Assert.Single(board.Areas[0].Removed).Title);
        Assert.Contains("OWNER CAROL DANVERS", board.Areas[1].Context);
        Assert.Contains("HOST 9", board.Areas[1].Context);
    }

    [Fact]
    public void ConcealedPileCardsExposeOnlyTheirBackAndCount()
    {
        CardDescriptor hidden = new(
            Id: null,
            CardBack.Encounter,
            FaceUp: false,
            Ready: true,
            Host: -1,
            Face: null);
        WorldDescriptor world = World(
            areas: [Area(2, "EncounterDeck", -1, [hidden, hidden])]);

        BoardCardPresentation pile = Assert.Single(
            Assert.Single(BoardPresentation.From(world).Areas).Cards);

        Assert.True(pile.Concealed);
        Assert.Equal(2, pile.Count);
        Assert.Null(pile.TargetId);
        Assert.Equal("2 concealed encounter cards", pile.Title);
        Assert.Equal("Identity and order hidden", pile.Subtitle);
        Assert.Empty(pile.Fields);
        Assert.Null(pile.FaceId);
        Assert.DoesNotContain("READY", pile.Status);
    }

    [Fact]
    public void VillainDeckIsPresentedAsUpcomingRatherThanSetAside()
    {
        BoardAreaPresentation area = Assert.Single(BoardPresentation.From(
            World(areas: [Area(2, "VillainDeck", -1, [Readable(7, "Rhino II")])]))
            .Areas);

        Assert.Equal("UPCOMING VILLAIN STAGES", area.Title);
        Assert.Contains("OUT OF PLAY", area.Context);
        Assert.Contains("ENTERS AFTER THE CURRENT STAGE", area.Context);
    }

    [Fact]
    public void PlayerAsideAreasUseSetAsideAndNemesisTableNames()
    {
        BoardPresentation board = BoardPresentation.From(World(
            areas:
            [
                Area(1, "AsideDeck", 0),
                Area(2, "AsideDeck", 0, [Readable(7, "Vulture")]),
                Area(3, "AsideDeck", 0),
            ],
            players: [new PlayerDescriptor(0, "Spider-Man", Eliminated: false)]));

        Assert.Equal("SPIDER-MAN'S SET-ASIDE AREA", board.Areas[0].Title);
        Assert.Equal("SPIDER-MAN'S NEMESIS SET", board.Areas[1].Title);
        Assert.Equal("SPIDER-MAN'S SET-ASIDE AREA", board.Areas[2].Title);
    }

    [Fact]
    public void AReadableCardInHandDoesNotClaimToBeReadyOrFaceDown()
    {
        BoardCardPresentation card = Assert.Single(Assert.Single(BoardPresentation.From(
            World(areas: [Area(1, "HandsArea", 0, [Readable(7, "Backflip")])]))
            .Areas).Cards);

        Assert.Equal(string.Empty, card.Status);
    }

    [Fact]
    public void AFaceDownCardInPlayRetainsOnlyItsAuthorizedHandleAndPublicState()
    {
        CardDescriptor hidden = new(
            Id: 41,
            CardBack.Player,
            FaceUp: false,
            Ready: false,
            Host: 7,
            Face: null);

        BoardCardPresentation card = Assert.Single(
            Assert.Single(BoardPresentation.From(
                World(areas: [Area(5, "EngagedEnemiesArea", 0, [hidden])])).Areas).Cards);

        Assert.True(card.Concealed);
        Assert.Equal(41, card.TargetId);
        Assert.Equal("Face-down player card", card.Title);
        Assert.Equal("EXHAUSTED  ·  FACE DOWN  ·  HOST 7", card.Status);
        Assert.Empty(card.Fields);
        Assert.Null(card.FaceId);
    }

    [Fact]
    public void ReadableCardsShowPrintedIdentityAndSortedLiveFields()
    {
        CardDescriptor readable = new(
            Id: 7,
            CardBack.Player,
            FaceUp: true,
            Ready: false,
            Host: -1,
            Face: new CardFaceDescriptor(
                "01010",
                "Captain Marvel",
                "Carol Danvers",
                CardKind.Hero,
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    ["thwart"] = 2,
                    ["amplify"] = 0,
                    ["is_exhaust"] = 0,
                    ["health"] = 11,
                    ["k_threat"] = 3,
                    ["t_avenger"] = 1,
                })
            {
                Traits = ["AVENGER", "AERIAL"],
                Cost = "3",
                PrintedStats = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["THW"] = "2",
                    ["ATK"] = "2",
                },
                Keywords = ["Steady"],
                RulesText = "Action: Draw a card.",
                Damage = 2,
                Counters = new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    ["energy"] = 3,
                },
            });

        BoardCardPresentation card = Assert.Single(
            Assert.Single(BoardPresentation.From(
                World(areas: [Area(1, "HeroArea", 0, [readable])])).Areas).Cards);

        Assert.Equal("01010", card.FaceId);

        Assert.False(card.Concealed);
        Assert.Equal("Captain Marvel", card.Title);
        Assert.Equal("Carol Danvers", card.Subtitle);
        Assert.Equal("HERO", card.Kind);
        Assert.Equal("EXHAUSTED", card.Status);
        Assert.Equal(["HEALTH", "THREAT", "THWART"], card.Fields.Select(field => field.Name));
        Assert.Equal(["11/13", "3", "2"], card.Fields.Select(field => field.Value));
        Assert.Equal(["AVENGER", "AERIAL"], card.Traits);
        Assert.Equal("3", card.Cost);
        Assert.Equal(["THW", "ATK"], card.PrintedStats.Select(field => field.Name));
        Assert.Equal(["Steady"], card.Keywords);
        Assert.Equal("Action: Draw a card.", card.RulesText);
        Assert.Equal(2, card.Damage);
        Assert.Equal("ENERGY", Assert.Single(card.Counters).Name);
        Assert.Equal("3", Assert.Single(card.Counters).Value);
    }

    [Fact]
    public void RebuildingFromANewerSnapshotDoesNotRetainEarlierFaces()
    {
        WorldDescriptor visible = World(
            areas: [Area(1, "HandsArea", 0, [Readable(7, "Swinging Web Kick")])]);
        WorldDescriptor hidden = World(
            areas:
            [
                Area(1, "HandsArea", 0,
                    [new CardDescriptor(null, CardBack.Player, false, true, -1, null)]),
            ]);

        BoardPresentation first = BoardPresentation.From(visible);
        BoardPresentation second = BoardPresentation.From(hidden);

        Assert.Equal("Swinging Web Kick", Assert.Single(first.Areas[0].Cards).Title);
        Assert.Equal("1 concealed player card", Assert.Single(second.Areas[0].Cards).Title);
        Assert.DoesNotContain(
            second.Areas.SelectMany(area => area.Cards),
            card => card.Title == "Swinging Web Kick");
    }

    [Fact]
    public void ScenarioAndTwoPlayerLanesFollowSeatOrderWithoutReorderingTheirAreas()
    {
        WorldDescriptor world = World(
            areas:
            [
                Area(10, "SecondPlayerArea", 1),
                Area(2, "VillainArea", -1),
                Area(8, "FirstPlayerArea", 0),
                Area(11, "SecondPlayerHandArea", 1),
                Area(99, "FutureQuantumArea", 7),
                Area(3, "EncounterDeck", -1),
            ],
            players:
            [
                new PlayerDescriptor(0, "Peter Parker", false),
                new PlayerDescriptor(1, "Carol Danvers", false),
            ]);

        BoardPresentation board = BoardPresentation.From(world);

        Assert.Equal(
            ["scenario", "player-0", "player-1", "other"],
            board.Lanes.Select(lane => lane.Key));
        Assert.Equal([2, 3], board.Lanes[0].Areas.Select(area => area.Id));
        Assert.Equal([8], board.Lanes[1].Areas.Select(area => area.Id));
        Assert.Equal([10, 11], board.Lanes[2].Areas.Select(area => area.Id));
        Assert.Equal([99], board.Lanes[3].Areas.Select(area => area.Id));
        Assert.Equal(
            world.Areas.Select(area => area.Id).Order(),
            board.Lanes.SelectMany(lane => lane.Areas).Select(area => area.Id).Order());
    }

    [Fact]
    public void HostedAreasFollowTheirVisibleHostAcrossSeatCoordinates()
    {
        WorldDescriptor world = World(
            areas:
            [
                Area(20, "AdditionalDeck", 1, [Readable(8, "Nested Host")], host: 7),
                Area(30, "NestedArea", 1, host: 8),
                Area(10, "VillainArea", -1, [Readable(7, "Rhino")]),
            ],
            players: [new PlayerDescriptor(1, "Carol Danvers", false)]);

        BoardLanePresentation lane = Assert.Single(BoardPresentation.From(world).Lanes);

        Assert.Equal("scenario", lane.Key);
        Assert.Equal([10, 20, 30], lane.Areas.Select(area => area.Id));
        Assert.Equal([0, 1, 2], lane.Areas.Select(area => area.Depth));
        Assert.Equal("Rhino", lane.Areas[1].HostedBy);
        Assert.Equal("Nested Host", lane.Areas[2].HostedBy);
    }

    [Fact]
    public void BrokenAndCyclicHostsRemainVisibleOnceInTheFallbackLane()
    {
        WorldDescriptor world = World(
            areas:
            [
                Area(1, "FutureA", 0, [Readable(10, "A")], host: 20),
                Area(2, "FutureB", 0, [Readable(20, "B")], host: 10),
                Area(3, "Orphan", 0, host: 999),
            ]);

        BoardLanePresentation lane = Assert.Single(BoardPresentation.From(world).Lanes);

        Assert.Equal("other", lane.Key);
        Assert.Equal([1, 2, 3], lane.Areas.Select(area => area.Id));
        Assert.Equal(3, lane.Areas.Select(area => area.Id).Distinct().Count());
    }

    private static WorldDescriptor World(
        IReadOnlyList<AreaDescriptor> areas,
        IReadOnlyList<PlayerDescriptor>? players = null) =>
        new(players ?? [new PlayerDescriptor(0, "Peter Parker", false)], areas, [],
            Outcome.Unfinished);

    private static AreaDescriptor Area(
        int id,
        string zone,
        int owner,
        IReadOnlyList<CardDescriptor>? cards = null,
        IReadOnlyList<CardDescriptor>? removed = null,
        int host = -1) =>
        new(id, zone, owner, host, cards ?? [], removed ?? []);

    private static CardDescriptor Readable(int id, string title) =>
        new(
            id,
            CardBack.Encounter,
            FaceUp: true,
            Ready: true,
            Host: -1,
            new CardFaceDescriptor(
                $"face-{id}", title, "", CardKind.Minion,
                new Dictionary<string, long>(StringComparer.Ordinal)));
}
