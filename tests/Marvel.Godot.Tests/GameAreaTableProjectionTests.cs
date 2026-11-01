using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

// Synthetic descriptors in the shape protocol 46 publishes for a separated Kang table; no game runs.
public sealed class GameAreaTableProjectionTests
{
    private static readonly string Reach = TableWording.For(TableFact.OutOfReach).Tooltip;
    private static readonly string Completion = TableWording.Rule(KangNoteKind.CompletionStoresDominion, 4);

    [Fact]
    public void TheCentralColumnStandsBetweenTheGameAreasInEngineOrder()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.True(table.Separated);
        Assert.Equal([5, 6, null, 7], table.Columns.Select(column => column.GameArea));
        GameAreaColumn central = table.Columns[2];
        Assert.True(central.Central);
        Assert.Equal([300], central.Schemes);
        Assert.Equal([402], central.Encounter);
        Assert.Equal([TableFact.Shared], central.Facts);
        Assert.Contains(TableWording.Rule(KangNoteKind.CentralTokensWaitFor4A, 4), central.Tooltip, StringComparison.Ordinal);
        Assert.Contains(TableWording.For(TableFact.Shared).Tooltip, central.Tooltip, StringComparison.Ordinal);
        Assert.Equal([101], table.Columns[0].Villains);
        Assert.Equal([201], table.Columns[0].Schemes);
        Assert.Equal([401], table.Columns[1].Encounter);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    public void TheMiddleRoundsRight(int areas, int middle) => Assert.Equal(middle, GameAreaTableProjection.Middle(areas));

    [Fact]
    public void ColumnsAreTitledByTheirPlayers()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.Equal(["Alice · Bob", "Carol", "Central", "Dan (retained)"], table.Columns.Select(column => column.Title));
    }

    [Fact]
    public void FactsComeFromTheSeatsAreaTheFocusTheDeparturesAndTheSeatsReach()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.Equal([TableFact.YourArea], table.Columns[0].Facts);
        Assert.Equal([TableFact.Acting, TableFact.Destination, TableFact.OutOfReach], table.Columns[1].Facts);
        Assert.Equal([TableFact.Retained, TableFact.Departing, TableFact.OutOfReach], table.Columns[3].Facts);
        Assert.Equal(1, table.Columns[1].ActingStep);
        Assert.Null(table.Columns[0].ActingStep);
    }

    [Fact]
    public void TheTooltipExplainsReachTheStepTheDepartureAndTheAnchoredRules()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.Equal(string.Join("\n", "Carol", Reach, TableWording.For(TableFact.StepThreat).Tooltip + " (Carol)",
            TableWording.For(TableFact.Destination).Tooltip, TableWording.Rule(KangNoteKind.CompletionStoresDominion, 4)),
            table.Columns[1].Tooltip);
        Assert.Equal(string.Join("\n", "Dan (retained)", Reach,
            TableWording.Departure("Kang (II)", [], completed: true, reunion: false)), table.Columns[3].Tooltip);
        Assert.Equal(string.Join("\n", "Alice · Bob", Completion), table.Columns[0].Tooltip);
    }

    [Fact]
    public void AnotherSeatSeesItsOwnAreaAsYoursAndTheFirstAreaAsOutOfReach()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 2)!;
        Assert.Equal([TableFact.OutOfReach], table.Columns[0].Facts);
        Assert.Equal([TableFact.YourArea, TableFact.Acting, TableFact.Destination], table.Columns[1].Facts);
    }

    [Fact]
    public void AnAreaWithoutUnreachableCardsIsNotMarkedOutOfReach()
    {
        BoardPresentation board = Board();
        board = board with { Players = [.. board.Players.Select(player => player with { OutOfReach = [], OutOfReachReason = null })] };
        GameAreaTable table = GameAreaTableProjection.From(board, 0)!;
        Assert.DoesNotContain(table.Columns, column => column.Facts.Contains(TableFact.OutOfReach));
        Assert.Empty(table.OutOfReach);
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
        GameAreaColumn column = GameAreaTableProjection.From(board, 0)!.Columns[1];
        Assert.Contains(TableFact.OutOfReach, column.Facts);
        Assert.Contains(Reach, column.Tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void ADepartureIsWordedFromItsCauseAndWhetherTheTableReunites()
    {
        BoardPresentation board = Board();
        board = board with { Kang = board.Kang! with { Departures = [new(103, 7, [], null, [], true)] } };
        GameAreaTable table = GameAreaTableProjection.From(board, 0)!;
        Assert.Contains(TableWording.Departure("Kang (II)", [], completed: false, reunion: true), table.Columns[3].Tooltip,
            StringComparison.Ordinal);
        Assert.DoesNotContain(table.Columns, column => column.Facts.Contains(TableFact.Destination));
    }

    [Fact]
    public void ADepartingGroupIsNamedByItsPlayers()
    {
        BoardPresentation board = Board();
        board = board with { Kang = board.Kang! with { Departures = [new(101, 5, [0, 1], 201, [6], false)] } };
        GameAreaTable table = GameAreaTableProjection.From(board, 0)!;
        Assert.Contains(TableWording.Departure("Card 101", ["Alice", "Bob"], completed: true, reunion: false),
            table.Columns[0].Tooltip, StringComparison.Ordinal);
        Assert.Equal([TableFact.YourArea, TableFact.Departing], table.Columns[0].Facts);
    }

    [Fact]
    public void TheRulesAreEachRuleOnceTableRulesFirst()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 0)!;
        Assert.Equal([TableWording.Rule(KangNoteKind.AreaIsolation, 4), TableWording.Rule(KangNoteKind.StepsInTurnOrder, 4),
            TableWording.For(TableFact.Shared).Tooltip, TableWording.Rule(KangNoteKind.CentralTokensWaitFor4A, 4), Completion],
            table.Rules);
        Assert.Equal(new HashSet<int>([102, 202, 401, 103, 203]), table.OutOfReach);
    }

    [Fact]
    public void TheTrackerCountsTheKangIIStillStandingAndLeavingAndTheStoredDominions()
    {
        TrackerState tracker = GameAreaTableProjection.From(Board(), 0)!.Tracker;
        Assert.Equal(("Kang (II)", 2, 3, 1), (tracker.Label, tracker.Filled, tracker.Total, tracker.Count));
        Assert.Equal(TableWording.For(TableFact.StoredDominions), tracker.Counter);
        Assert.Equal(TableWording.Progress(Board().Kang!), tracker.Tooltip);
    }

    [Fact]
    public void AnEliminatedSeatHasNoAreaOfItsOwn()
    {
        GameAreaTable table = GameAreaTableProjection.From(Board(), 3)!;
        Assert.DoesNotContain(table.Columns, column => column.Facts.Contains(TableFact.YourArea));
    }

    [Theory]
    [InlineData(1, 0, false)]
    [InlineData(3, 0, false)]
    [InlineData(3, 2, true)]
    public void AWholeKangTableHasATrackerButNoColumnsOrPips(int stage, int stored, bool counted)
    {
        BoardPresentation board = Board() with
        {
            GameAreas = [new GameAreaDescriptor(1, [-1, 0, 1]) { Central = true, Villains = [100] }],
            Kang = new KangTableDescriptor(stage, 0, stored, [], null, []),
        };
        GameAreaTable table = GameAreaTableProjection.From(board, 0)!;
        Assert.False(table.Separated);
        Assert.Equal(stage == 1 ? "Kang (I)" : "Kang (III)", table.Tracker.Label);
        Assert.Equal(0, table.Tracker.Total);
        Assert.Equal(counted, table.Tracker.Counter is not null);
        Assert.Equal(TableWording.Progress(board.Kang!), table.Tracker.Tooltip);
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

    private static readonly int[] OutOfReach = [102, 202, 401, 103, 203];

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
            new PlayerDescriptor(0, "Alice", false) { GameArea = 5, OutOfReach = OutOfReach, OutOfReachReason = ReachReason.OtherGameArea },
            new PlayerDescriptor(1, "Bob", false) { GameArea = 5, OutOfReach = OutOfReach, OutOfReachReason = ReachReason.OtherGameArea },
            new PlayerDescriptor(2, "Carol", false) { GameArea = 6, OutOfReach = [101, 201, 103, 203], OutOfReachReason = ReachReason.OtherGameArea },
            new PlayerDescriptor(3, "Dan", true),
        ],
        Kang = new KangTableDescriptor(2, 2, 1, [300], null, [new(103, 7, [], 203, [6], false)])
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
