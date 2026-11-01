using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

// Synthetic descriptors in the shape protocol 46 publishes for a separated Kang table; no game runs.
public sealed class GameAreaTableProjectionTests
{
    private static readonly string Iso = TableWording.For(TableFact.OutOfReach).Tooltip;
    private static readonly string Completion = TableWording.Rule(KangNoteKind.CompletionStoresDominion, 4);
    private static readonly string Tokens = TableWording.Rule(KangNoteKind.CentralTokensWaitFor4A, 4);
    private static readonly string StepOne = TableWording.For(TableFact.StepThreat).Tooltip;
    private static readonly string Leaving = TableWording.Departure("Kang (II)", [], completed: true, reunion: false);

    [Fact]
    public void ColumnsFollowTheCentralStageThenEachGameAreaInEngineOrder()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.True(table.Separated);
        Assert.Equal([null, 5, 6, 7], table.Columns.Select(column => column.GameArea));
        GameAreaColumn central = table.Columns[0];
        Assert.Equal([300], central.Schemes);
        Assert.Equal([Tokens], central.Details);
        Assert.Equal([402], central.Encounter);
        Assert.Equal([101], table.Columns[1].Villains);
        Assert.Equal([201], table.Columns[1].Schemes);
        Assert.Equal([401], table.Columns[2].Encounter);
        Assert.Equal("Alice · Bob", table.Columns[1].Heading);
        Assert.Equal("Dan (retained)", table.Columns[3].Heading);
    }

    [Fact]
    public void MarkersComeFromTheSeatsAreaTheFocusTheDeparturesAndTheSeatsReach()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.Equal([GameAreaMarker.Yours], table.Columns[1].Markers);
        Assert.Equal([GameAreaMarker.Acting, GameAreaMarker.OutOfReach], table.Columns[2].Markers);
        Assert.Equal([Iso, StepOne, Completion], table.Columns[2].Details);
        Assert.Equal([GameAreaMarker.Retained, GameAreaMarker.Departing, GameAreaMarker.OutOfReach], table.Columns[3].Markers);
        Assert.Equal([Iso, Leaving], table.Columns[3].Details);
        Assert.Equal([Completion], table.Columns[1].Details);
        Assert.Equal("YOUR AREA", table.Columns[1].MarkerText);
        Assert.Equal("ACTING NOW · OUT OF REACH", table.Columns[2].MarkerText);
    }

    [Fact]
    public void AnotherSeatSeesItsOwnAreaAsYoursAndTheFirstAreaAsOutOfReach()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 2)!;
        Assert.Equal([GameAreaMarker.OutOfReach], table.Columns[1].Markers);
        Assert.Equal([GameAreaMarker.Yours, GameAreaMarker.Acting], table.Columns[2].Markers);
        Assert.Equal([Iso, Completion], table.Columns[1].Details);
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
        Assert.Equal(TableWording.For(TableFact.Shared).Tooltip, table.SharedPiles);
        Assert.Equal([TableWording.Rule(KangNoteKind.AreaIsolation, 4), TableWording.Rule(KangNoteKind.StepsInTurnOrder, 4),
            TableWording.For(TableFact.Shared).Tooltip], table.Rules);
        Assert.Equal(TableWording.Progress(Board().Kang!), table.Progress);
        Assert.Equal(Reach.Order(), table.OutOfReach.Order());
    }

    [Fact]
    public void AnAreaHoldingOnlySideSchemesIsStillOutOfReachForItsReason()
    {
        // The engine lists every addressable card in another area, side schemes included.
        BoardPresentation board = Board();
        board = board with
        {
            GameAreas = [.. board.GameAreas.Select(area => area.Id == 6 ? area with { Villains = [], MainSchemes = [] } : area)],
            Players = [.. board.Players.Select(player => player.Seat == 0 ? player with { OutOfReach = [401] } : player)],
        };
        GameAreaColumn column = GameAreaTableProjection.From(board, 0)!.Columns[2];
        Assert.Equal([GameAreaMarker.Acting, GameAreaMarker.OutOfReach], column.Markers);
        Assert.Equal(Iso, column.Details[0]);
    }

    [Fact]
    public void ADepartureIsWordedFromItsCauseAndWhetherTheTableReunites()
    {
        BoardPresentation board = Board();
        board = board with { Kang = board.Kang! with { Departures = [new(103, 7, [], null, [], true)] } };
        Assert.Equal([Iso, TableWording.Departure("Kang (II)", [], completed: false, reunion: true)],
            GameAreaTableProjection.From(board, 0)!.Columns[3].Details);
    }

    [Fact]
    public void ADepartureComesBeforeTheNotesAnchoredToItsSchemes()
    {
        BoardPresentation board = Board();
        board = board with { Kang = board.Kang! with { Departures = [new(101, 5, [0, 1], 201, [6], false)] } };
        Assert.Equal([TableWording.Departure("Card 101", ["Alice", "Bob"], completed: true, reunion: false), Completion],
            GameAreaTableProjection.From(board, 0)!.Columns[1].Details);
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
            Kang = new KangTableDescriptor(1, 0, 0, [], null, []),
        };
        GameAreaTable table = GameAreaTableProjection.From(board, 0)!;
        Assert.False(table.Separated);
        Assert.Equal("Defeat Kang (I) to split the table into separate game areas.", table.Progress);
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
            new GameAreaDescriptor(5, [0, 1]) { Villains = [101], MainSchemes = [201] },
            new GameAreaDescriptor(6, [2]) { Villains = [102], MainSchemes = [202] },
            new GameAreaDescriptor(7, []) { Villains = [103], MainSchemes = [203], Retained = true, FormerSeats = [3] },
        ],
        Players =
        [
            new PlayerDescriptor(0, "Alice", false) { GameArea = 5, OutOfReach = Reach, OutOfReachReason = ReachReason.OtherGameArea },
            new PlayerDescriptor(1, "Bob", false) { GameArea = 5, OutOfReach = Reach, OutOfReachReason = ReachReason.OtherGameArea },
            new PlayerDescriptor(2, "Carol", false) { GameArea = 6, OutOfReach = [101, 201, 103, 203], OutOfReachReason = ReachReason.OtherGameArea },
            new PlayerDescriptor(3, "Dan", true),
        ],
        Kang = new KangTableDescriptor(2, 2, 1, [300], null, [new(103, 7, [], 203, [5, 6], false)])
        {
            Acting = new VillainPhaseFocusDescriptor(1, 2, 6),
            StartingPlayers = 4,
            Notes =
            [
                new(KangNoteKind.AreaIsolation, null),
                new(KangNoteKind.StepsInTurnOrder, null),
                new(KangNoteKind.SharedEncounterDeck, null),
                new(KangNoteKind.CentralTokensWaitFor4A, 300),
                new(KangNoteKind.CompletionStoresDominion, 201),
                new(KangNoteKind.CompletionStoresDominion, 202),
            ],
        },
    };

    private static BoardAreaPresentation Area(int id, string zone, int seat, BoardCardPresentation card) =>
        new(id, zone, "", [card], []) { Zone = zone, Seat = seat };

    private static BoardCardPresentation Card(int id, int? area) =>
        new(id, 1, false, $"Card {id}", "", "SIDE SCHEME", "", []) { GameArea = area };
}
