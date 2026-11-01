using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ClientGameAreaValidationTests
{
    [Fact]
    public void ACompleteSeparatedTableIsAccepted()
    {
        Assert.True(ClientGameAreaValidation.Valid(World()));
        Assert.True(ClientGameAreaValidation.Valid(World() with { Kang = null }));
        Assert.True(ClientResponseValidation.HasCompleteBoard(World()));
    }

    [Fact]
    public void ARetainedAreaAnEliminatedSeatAndAnOrdinaryTableAreAccepted()
    {
        var world = World();
        var retained = world with
        {
            Players = [world.Players[0], world.Players[1], new PlayerDescriptor(2, "p2", true)],
            Areas = [world.Areas[0] with { Cards = [.. world.Areas[0].Cards, Card(15, 9)] }, world.Areas[1], world.Areas[2]],
            GameAreas = [.. world.GameAreas, new GameAreaDescriptor(9, []) { Retained = true, FormerSeats = [2],
                Villains = [15], Label = "p2's retained game area" }],
            Kang = world.Kang! with { StartingPlayers = 3 },
        };
        var ordinary = new WorldDescriptor(
            [new PlayerDescriptor(0, "p0", false) { GameArea = 0 }],
            [new AreaDescriptor(2, "VillainArea", -1, -1, [Card(10, 0)], [])],
            [new GameAreaDescriptor(0, [-1, 0]) { Central = true, Villains = [10], Label = "the game area (Rhino)" }],
            Outcome.Unfinished);

        Assert.True(ClientGameAreaValidation.Valid(retained));
        Assert.True(ClientResponseValidation.HasCompleteBoard(retained));
        Assert.True(ClientGameAreaValidation.Valid(ordinary));
        Assert.True(ClientResponseValidation.HasCompleteBoard(ordinary));
    }

    [Theory]
    [InlineData("unknown villain")]
    [InlineData("unknown main scheme")]
    [InlineData("unknown former seat")]
    [InlineData("central without the villain play area")]
    [InlineData("retained with a seat")]
    [InlineData("duplicate game area")]
    [InlineData("null label")]
    [InlineData("unknown out-of-reach card")]
    [InlineData("duplicate out-of-reach card")]
    [InlineData("stage 0")]
    [InlineData("stage 4")]
    [InlineData("negative remaining")]
    [InlineData("negative dominions")]
    [InlineData("unknown central scheme")]
    [InlineData("unknown pending split")]
    [InlineData("unknown departing villain")]
    [InlineData("unknown source")]
    [InlineData("unknown departing seat")]
    [InlineData("unknown completed scheme")]
    [InlineData("unknown destination")]
    [InlineData("destination is the source")]
    [InlineData("reunion with a destination")]
    [InlineData("no destination without reunion")]
    [InlineData("duplicate departure")]
    [InlineData("null departure summary")]
    [InlineData("acting step 3")]
    [InlineData("unknown acting seat")]
    [InlineData("unknown acting area")]
    [InlineData("starting players differ")]
    [InlineData("encounter deck names another pile")]
    [InlineData("encounter discard names another pile")]
    [InlineData("null progress")]
    [InlineData("out-of-reach card in the seat's own area")]
    [InlineData("seat in no game area with reach limits")]
    [InlineData("villain located in another area")]
    [InlineData("main scheme located in another area")]
    [InlineData("acting area is not the seat's")]
    [InlineData("departing villain in another area")]
    [InlineData("undefined note kind")]
    [InlineData("duplicate note")]
    [InlineData("anchored note without a card")]
    [InlineData("table note with a card")]
    [InlineData("note names an unknown card")]
    [InlineData("null note text")]
    [InlineData("null notes list")]
    public void AFactNamingSomethingTheResponseDoesNotListIsRejected(string defect)
    {
        // Synthetic wire shape: the client checks references only; reach, departures and
        // progress are engine facts it never recomputes.
        var world = World();
        var kang = world.Kang!;
        var departure = kang.Departures[0];
        var area = world.GameAreas[0];
        WorldDescriptor Area(GameAreaDescriptor changed) => world with { GameAreas = [changed, world.GameAreas[1]] };
        WorldDescriptor Kang(KangTableDescriptor changed) => world with { Kang = changed };
        WorldDescriptor Departure(KangDepartureDescriptor changed) => Kang(kang with { Departures = [changed] });
        WorldDescriptor Notes(KangNoteDescriptor added) => Kang(kang with { Notes = [.. kang.Notes, added] });
        var broken = defect switch
        {
            "unknown villain" => Area(area with { Villains = [99] }),
            "unknown main scheme" => Area(area with { MainSchemes = [99] }),
            "unknown former seat" => Area(area with { FormerSeats = [5] }),
            "central without the villain play area" => Area(area with { Central = true }),
            "retained with a seat" => Area(area with { Retained = true }),
            "duplicate game area" => world with { GameAreas = [area, world.GameAreas[1], world.GameAreas[1] with { Label = "again" }] },
            "null label" => Area(area with { Label = null! }),
            "unknown out-of-reach card" => world with { Players = [world.Players[0] with { OutOfReach = [99] }, world.Players[1]] },
            "duplicate out-of-reach card" => world with { Players = [world.Players[0] with { OutOfReach = [11, 11] }, world.Players[1]] },
            "stage 0" => Kang(kang with { Stage = 0 }),
            "stage 4" => Kang(kang with { Stage = 4 }),
            "negative remaining" => Kang(kang with { RemainingLocalKangs = -1 }),
            "negative dominions" => Kang(kang with { StoredDominions = -1 }),
            "unknown central scheme" => Kang(kang with { CentralSchemes = [99] }),
            "unknown pending split" => Kang(kang with { PendingSplit = 99 }),
            "unknown departing villain" => Departure(departure with { Villain = 99 }),
            "unknown source" => Departure(departure with { SourceGameArea = 99 }),
            "unknown departing seat" => Departure(departure with { Seats = [5] }),
            "unknown completed scheme" => Departure(departure with { CompletedScheme = 99 }),
            "unknown destination" => Departure(departure with { Destinations = [99] }),
            "destination is the source" => Departure(departure with { Destinations = [7] }),
            "reunion with a destination" => Departure(departure with { Reunion = true }),
            "no destination without reunion" => Departure(departure with { Destinations = [] }),
            "duplicate departure" => Kang(kang with { Departures = [departure, departure] }),
            "null departure summary" => Departure(departure with { Summary = null! }),
            "acting step 3" => Kang(kang with { Acting = kang.Acting! with { Step = 3 } }),
            "unknown acting seat" => Kang(kang with { Acting = kang.Acting! with { Seat = 5 } }),
            "unknown acting area" => Kang(kang with { Acting = kang.Acting! with { GameArea = 99 } }),
            "starting players differ" => Kang(kang with { StartingPlayers = 3 }),
            "encounter deck names another pile" => Kang(kang with { EncounterDeck = 21 }),
            "encounter discard names another pile" => Kang(kang with { EncounterDiscard = 20 }),
            "null progress" => Kang(kang with { Progress = null! }),
            "out-of-reach card in the seat's own area" => world with { Players = [world.Players[0] with { OutOfReach = [10] }, world.Players[1]] },
            "seat in no game area with reach limits" => world with { Players = [world.Players[0] with { GameArea = null }, world.Players[1]] },
            "villain located in another area" => Area(area with { Villains = [12] }),
            "main scheme located in another area" => Area(area with { MainSchemes = [13] }),
            "acting area is not the seat's" => Kang(kang with { Acting = kang.Acting! with { GameArea = 7 } }),
            "departing villain in another area" => Departure(departure with { Villain = 12 }),
            "undefined note kind" => Notes(new KangNoteDescriptor((KangNoteKind)99, null, "x")),
            "duplicate note" => Notes(kang.Notes[0]),
            "anchored note without a card" => Notes(new KangNoteDescriptor(KangNoteKind.CentralTokensWaitFor4A, null, "x")),
            "table note with a card" => Notes(new KangNoteDescriptor(KangNoteKind.AreaIsolation, 14, "x")),
            "note names an unknown card" => Notes(new KangNoteDescriptor(KangNoteKind.CompletionStoresDominion, 99, "x")),
            "null note text" => Kang(kang with { Notes = [kang.Notes[0] with { Text = null! }] }),
            "null notes list" => Kang(kang with { Notes = null! }),
            _ => throw new ArgumentOutOfRangeException(nameof(defect)),
        };

        Assert.False(ClientGameAreaValidation.Valid(broken));
        Assert.False(ClientResponseValidation.HasCompleteBoard(broken));
    }

    private static CardDescriptor Card(int id, int? area) => new(id, CardBack.Encounter, true, true, -1, null)
    {
        Location = new CardLocationDescriptor(2, "VillainArea", -1, -1) { GameArea = area },
    };

    private static WorldDescriptor World()
    {
        return new WorldDescriptor(
            [new PlayerDescriptor(0, "p0", false) { GameArea = 7, OutOfReach = [12, 13] },
                new PlayerDescriptor(1, "p1", false) { GameArea = 8, OutOfReach = [10, 11] }],
            [new AreaDescriptor(2, "VillainArea", 0, -1, [Card(10, 7), Card(11, 7), Card(12, 8), Card(13, 8), Card(14, null)], []),
                new AreaDescriptor(20, "EncounterDeck", -1, -1, [], []),
                new AreaDescriptor(21, "EncounterDiscardPile", -1, -1, [], [])],
            [new GameAreaDescriptor(7, [0]) { Villains = [10], MainSchemes = [11], Label = "p0's game area" },
                new GameAreaDescriptor(8, [1]) { Villains = [12], MainSchemes = [13], Label = "p1's game area" }],
            Outcome.Unfinished)
        {
            Kang = new KangTableDescriptor(2, 1, 1, [14], null,
                [new KangDepartureDescriptor(10, 7, [0], 11, [8], false, "p0 leaves.")])
            {
                Acting = new VillainPhaseFocusDescriptor(2, 1, 8, "step 2"),
                StartingPlayers = 2,
                EncounterDeck = 20,
                EncounterDiscard = 21,
                Progress = "1 Kang (II) remains.",
                Notes = [new(KangNoteKind.AreaIsolation, null, "isolation"),
                    new(KangNoteKind.CentralTokensWaitFor4A, 14, "tokens"),
                    new(KangNoteKind.CompletionStoresDominion, 11, "completion")],
            },
        };
    }
}
