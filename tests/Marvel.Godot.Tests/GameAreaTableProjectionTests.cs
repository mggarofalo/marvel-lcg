using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

// Synthetic descriptors in the shape protocol 45 publishes for a separated Kang table; no game runs.
public sealed class GameAreaTableProjectionTests
{
    [Fact]
    public void ColumnsFollowTheCentralStageThenEachGameAreaInEngineOrder()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.True(table.Separated);
        Assert.Equal([null, 5, 6, 7], table.Columns.Select(column => column.GameArea));
        GameAreaColumn central = table.Columns[0];
        Assert.Equal([300], central.Schemes);
        Assert.Equal(["tokens"], central.Details);
        Assert.Equal([402], central.Encounter);
        Assert.Equal([101], table.Columns[1].Villains);
        Assert.Equal([201], table.Columns[1].Schemes);
        Assert.Equal([401], table.Columns[2].Encounter);
        Assert.Equal("Alice and Bob's game area (Kang (II))", table.Columns[1].Heading);
    }

    [Fact]
    public void MarkersComeFromTheSeatsAreaTheFocusTheDeparturesAndTheSeatsReach()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.Equal([GameAreaMarker.Yours], table.Columns[1].Markers);
        Assert.Equal([GameAreaMarker.Acting, GameAreaMarker.OutOfReach], table.Columns[2].Markers);
        Assert.Equal(["acting summary", "complete 202", "Out of reach for Alice. iso"], table.Columns[2].Details);
        Assert.Equal([GameAreaMarker.Retained, GameAreaMarker.Departing, GameAreaMarker.OutOfReach], table.Columns[3].Markers);
        Assert.Equal(["leaving 7", "Out of reach for Alice. iso"], table.Columns[3].Details);
        Assert.Equal(["complete 201"], table.Columns[1].Details);
        Assert.Equal("YOUR AREA", table.Columns[1].MarkerText);
        Assert.Equal("ACTING NOW · OUT OF REACH", table.Columns[2].MarkerText);
    }

    [Fact]
    public void AnotherSeatSeesItsOwnAreaAsYoursAndTheFirstAreaAsOutOfReach()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 2)!;
        Assert.Equal([GameAreaMarker.OutOfReach], table.Columns[1].Markers);
        Assert.Equal([GameAreaMarker.Yours, GameAreaMarker.Acting], table.Columns[2].Markers);
        Assert.Equal(["Out of reach for Carol. iso"], table.Columns[1].Details.Skip(1));
    }

    [Fact]
    public void AnAreaWithoutUnreachableCardsIsNotMarkedOutOfReach()
    {
        BoardPresentation board = Board();
        board = board with { Players = [.. board.Players.Select(player => player with { OutOfReach = [] })] };
        GameAreaTable table = GameAreaTableProjection.From(board, 0)!;
        Assert.DoesNotContain(table.Columns, column => column.Markers.Contains(GameAreaMarker.OutOfReach));
        Assert.Empty(table.OutOfReach);
    }

    [Fact]
    public void WholeTableTextComesFromTheNotesByKind()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.Equal("shared", table.SharedPiles);
        Assert.Equal(["iso", "steps", "shared"], table.Rules);
        Assert.Equal("acting summary", table.Acting);
        Assert.Equal("progress 1 Kang's Dominion stored under stage 4A.", table.Progress);
        Assert.Equal(Reach.Order(), table.OutOfReach.Order());
    }

    [Theory]
    [InlineData(0, "progress")]
    [InlineData(2, "progress 2 Kang's Dominions stored under stage 4A.")]
    public void ProgressCountsStoredDominionsOnlyWhenThereAreAny(int stored, string expected)
    {
        BoardPresentation board = Board();
        board = board with { Kang = board.Kang! with { StoredDominions = stored } };
        Assert.Equal(expected, GameAreaTableProjection.From(board, 0)!.Progress);
    }

    [Fact]
    public void AReunionDepartureSaysTheTableReunites()
    {
        BoardPresentation board = Board();
        board = board with { Kang = board.Kang! with { Departures = [new(103, 7, [], 203, [], true, "leaving 7")] } };
        Assert.Equal("leaving 7 No game area remains for this group to join, so the table reunites at stage 4A.",
            GameAreaTableProjection.From(board, 0)!.Columns[3].Details[0]);
    }

    [Fact]
    public void AnEliminatedSeatHasNoAreaOfItsOwn()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 3)!;
        Assert.DoesNotContain(table.Columns, column => column.Markers.Contains(GameAreaMarker.Yours));
    }

    [Fact]
    public void AWholeKangTableHasProgressButNoColumns()
    {
        BoardPresentation board = Board() with
        {
            GameAreas = [new GameAreaDescriptor(1, [-1, 0, 1]) { Central = true, Villains = [100] }],
            Kang = new KangTableDescriptor(1, 0, 0, [], null, []) { Progress = "Defeat Kang (I)." },
        };
        GameAreaTable table = GameAreaTableProjection.From(board, 0)!;
        Assert.False(table.Separated);
        Assert.Equal("Defeat Kang (I).", table.Progress);
    }

    [Fact]
    public void AnyOtherGameHasNoGameAreaPresentation()
    {
        var board = new BoardPresentation([])
        {
            GameAreas = [new GameAreaDescriptor(1, [-1, 0]) { Central = true }],
            Players = [new PlayerDescriptor(0, "Alice", false) { GameArea = 1 }],
        };
        Assert.Null(GameAreaTableProjection.From(board, 0));
    }

    private static readonly int[] Reach = [102, 202, 401, 103, 203];

    private static BoardPresentation Board() => new([
        Area(1, "SideSchemesArea", 2, Card(401, 6)),
        Area(2, "SideSchemesArea", -1, Card(402, null)),
        Area(3, "VillainArea", 0, Card(101, 5)),
    ])
    {
        GameAreas =
        [
            new GameAreaDescriptor(5, [0, 1]) { Villains = [101], MainSchemes = [201], Label = "Alice and Bob's game area (Kang (II))" },
            new GameAreaDescriptor(6, [2]) { Villains = [102], MainSchemes = [202], Label = "Carol's game area (Kang (II))" },
            new GameAreaDescriptor(7, []) { Villains = [103], MainSchemes = [203], Retained = true, FormerSeats = [3],
                Label = "Dan's retained game area (Kang (II))" },
        ],
        Players =
        [
            new PlayerDescriptor(0, "Alice", false) { GameArea = 5, OutOfReach = Reach },
            new PlayerDescriptor(1, "Bob", false) { GameArea = 5, OutOfReach = Reach },
            new PlayerDescriptor(2, "Carol", false) { GameArea = 6, OutOfReach = [101, 201, 103, 203] },
            new PlayerDescriptor(3, "Dan", true),
        ],
        Kang = new KangTableDescriptor(2, 2, 1, [300], null, [new(103, 7, [], 203, [5, 6], false, "leaving 7")])
        {
            Acting = new VillainPhaseFocusDescriptor(1, 2, 6, "acting summary"),
            Progress = "progress",
            Notes =
            [
                new(KangNoteKind.AreaIsolation, null, "iso"),
                new(KangNoteKind.StepsInTurnOrder, null, "steps"),
                new(KangNoteKind.SharedEncounterDeck, null, "shared"),
                new(KangNoteKind.CentralTokensWaitFor4A, 300, "tokens"),
                new(KangNoteKind.CompletionStoresDominion, 201, "complete 201"),
                new(KangNoteKind.CompletionStoresDominion, 202, "complete 202"),
            ],
        },
    };

    private static BoardAreaPresentation Area(int id, string zone, int seat, BoardCardPresentation card) =>
        new(id, zone, "", [card], []) { Zone = zone, Seat = seat };

    private static BoardCardPresentation Card(int id, int? area) =>
        new(id, 1, false, $"Card {id}", "", "SIDE SCHEME", "", []) { GameArea = area };
}
