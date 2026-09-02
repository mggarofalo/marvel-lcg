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
        Assert.DoesNotContain("READY", pile.Status);
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
                    ["hitPoints"] = 11,
                    ["k_energy"] = 3,
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
                World(areas: [Area(1, "IdentityArea", 0, [readable])])).Areas).Cards);

        Assert.False(card.Concealed);
        Assert.Equal("Captain Marvel", card.Title);
        Assert.Equal("Carol Danvers", card.Subtitle);
        Assert.Equal("HERO", card.Kind);
        Assert.Equal("EXHAUSTED  ·  FACE UP", card.Status);
        Assert.Equal(["HIT POINTS", "THWART"], card.Fields.Select(field => field.Name));
        Assert.Equal(["11", "2"], card.Fields.Select(field => field.Value));
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
