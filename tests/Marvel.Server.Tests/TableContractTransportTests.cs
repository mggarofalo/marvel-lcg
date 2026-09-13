using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class TableContractTransportTests
{
    [Fact]
    public void VersionFifteenRoundTripsStructuredTableContractsWithoutCollapsingIdsOrOrder()
    {
        CardDescriptor first = Card(17, "Duplicate");
        CardDescriptor second = Card(23, "Duplicate");
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "Zero", false), new PlayerDescriptor(1, "One", false)],
            [new AreaDescriptor(4, "AlliesArea", 1, -1, [first, second], [])],
            [],
            Outcome.Unfinished)
        {
            Table = new TableContextDescriptor(
                PromptOwner: 0,
                ViewedPrivateSeat: 0,
                ActivePlayer: 1,
                FirstPlayer: 0,
                PublicFocusSeat: 1),
            PlayerSummaries = [new PlayerSummaryDescriptor(1, 23, "Hero", 10, [17], [], [23])],
            Relationships =
            [
                new TableRelationshipDescriptor(RelationshipKind.Attachment, 17, 23),
                new TableRelationshipDescriptor(RelationshipKind.OfferedTarget, 23, 17),
            ],
        };
        var response = new EngineResponse(
            EngineProtocol.Version, "table", "game", Capability: null, Prompt: null, Events: [], world);

        byte[] json = EngineJson.Write(response);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement table = document.RootElement.GetProperty("world").GetProperty("table");
        EngineResponse restored = EngineJson.ReadResponse(json);

        Assert.Equal(15, response.Version);
        Assert.Equal(1, table.GetProperty("active_player").GetInt32());
        Assert.Equal([17, 23], restored.World!.Areas[0].Cards.Select(card => card.Id));
        Assert.Equal([17, 23], restored.World.Relationships.Select(relationship => relationship.Subject));
        Assert.Equal([23, 17], restored.World.Relationships.Select(relationship => relationship.Related));
        Assert.Equal(0, restored.World.Table?.PromptOwner);
        Assert.Equal(1, restored.World.Table?.PublicFocusSeat);
    }

    private static CardDescriptor Card(int id, string title) => new(
        id,
        CardBack.Player,
        FaceUp: true,
        Ready: true,
        Host: -1,
        new CardFaceDescriptor($"face-{id}", title, string.Empty, CardKind.Ally,
            new Dictionary<string, long>(StringComparer.Ordinal)))
    {
        Location = new CardLocationDescriptor(4, "AlliesArea", 1, -1),
        State = new CardStateDescriptor(true, 0, null,
            new Dictionary<string, long>(StringComparer.Ordinal),
            new Dictionary<string, long>(StringComparer.Ordinal), []),
    };
}
