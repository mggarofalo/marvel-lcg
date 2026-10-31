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
}
