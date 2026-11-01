using System.Text;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class GameAreaTransportTests
{
    [Fact]
    public void SeatAndCardGameAreasCrossTheWireIncludingTheCentralNone()
    {
        // Protocol 44: a separated Kang table names each seat's and card's game
        // area; a central card such as stage 2B is in none.
        CardDescriptor Card(int id, int? area) => new(id, CardBack.Encounter, true, true, -1, null)
        {
            Location = new CardLocationDescriptor(1, "VillainArea", -1, -1) { GameArea = area },
        };
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "p0", false) { GameArea = 7 }, new PlayerDescriptor(1, "p1", false) { GameArea = 8 }],
            [new AreaDescriptor(1, "VillainArea", -1, -1, [Card(10, 7), Card(11, null)], [])],
            [new GameAreaDescriptor(7, [0]), new GameAreaDescriptor(8, [1])],
            Outcome.Unfinished);
        var response = new EngineResponse(EngineProtocol.Version, "areas", "game", null, null, [], world);

        byte[] json = EngineJson.Write(response);
        var restored = EngineJson.ReadResponse(json);

        Assert.Contains("\"game_area\":7", Encoding.UTF8.GetString(json), StringComparison.Ordinal);
        Assert.Equal([7, 8], restored.World!.Players.Select(player => player.GameArea));
        var cards = restored.World.Areas[0].Cards;
        Assert.Equal(7, cards[0].Location!.GameArea);
        Assert.Null(cards[1].Location!.GameArea);
    }

    [Fact]
    public void AreaSummariesReachAndKangFactsSurviveTheStrictCodec()
    {
        // Protocol 45: each game area's villains, main schemes, central or retained role and
        // label; each seat's out-of-reach cards; and Kang's progress, departures and acting area.
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "p0", false) { GameArea = 7, OutOfReach = [12] }],
            [new AreaDescriptor(1, "VillainArea", 0, -1, [], [])],
            [new GameAreaDescriptor(7, [0]) { Villains = [10], MainSchemes = [11], Label = "p0's game area" },
                new GameAreaDescriptor(8, []) { Retained = true, FormerSeats = [1], Label = "p1's retained game area" }],
            Outcome.Unfinished)
        {
            Kang = new KangTableDescriptor(2, 1, 1, [14], null,
                [new KangDepartureDescriptor(12, 8, [], 13, [], true, "p1's retained components leave.")])
            {
                Acting = new VillainPhaseFocusDescriptor(2, 0, 7, "step 2"),
                StartingPlayers = 2,
                EncounterDeck = 20,
                EncounterDiscard = 21,
                Progress = "1 Kang (II) remains.",
                Notes = ["note"],
            },
        };
        var response = new EngineResponse(EngineProtocol.Version, "kang", "game", null, null, [], world);

        byte[] json = EngineJson.Write(response);
        var restored = EngineJson.ReadResponse(json).World!;

        string text = Encoding.UTF8.GetString(json);
        foreach (string member in new[] { "\"out_of_reach\":[12]", "\"main_schemes\":[11]", "\"former_seats\":[1]",
            "\"stored_dominions\":1", "\"remaining_local_kangs\":1", "\"completed_scheme\":13", "\"reunion\":true" })
            Assert.Contains(member, text, StringComparison.Ordinal);
        Assert.Equal([12], restored.Players[0].OutOfReach);
        Assert.Equal([10], restored.GameAreas[0].Villains);
        Assert.Equal([11], restored.GameAreas[0].MainSchemes);
        Assert.Equal((true, false, "p1's retained game area"), (restored.GameAreas[1].Retained,
            restored.GameAreas[1].Central, restored.GameAreas[1].Label));
        Assert.Equal([1], restored.GameAreas[1].FormerSeats);
        var kang = restored.Kang!;
        Assert.Equal((2, 1, 1, 2, 20, 21), (kang.Stage, kang.RemainingLocalKangs, kang.StoredDominions,
            kang.StartingPlayers, kang.EncounterDeck, kang.EncounterDiscard));
        Assert.Equal(world.Kang.Acting, kang.Acting);
        var departure = Assert.Single(kang.Departures);
        Assert.Equal((12, 8, 13, true), (departure.Villain, departure.SourceGameArea, departure.CompletedScheme, departure.Reunion));
        Assert.Equal(["note"], kang.Notes);
        Assert.Equal(EngineJson.Write(response), EngineJson.Write(EngineJson.ReadResponse(json)));
    }

    [Fact]
    public void AnUnknownKangMemberIsRejected()
    {
        var world = new WorldDescriptor([], [], [], Outcome.Unfinished)
        {
            Kang = new KangTableDescriptor(1, 0, 0, [], null, []),
        };
        string text = Encoding.UTF8.GetString(EngineJson.Write(
            new EngineResponse(EngineProtocol.Version, "kang", "game", null, null, [], world)));
        string forged = text.Replace("\"stored_dominions\":0", "\"stored_dominions\":0,\"winner\":true", StringComparison.Ordinal);

        Assert.NotEqual(text, forged);
        Assert.Throws<System.Text.Json.JsonException>(() => EngineJson.ReadResponse(Encoding.UTF8.GetBytes(forged)));
    }
}
