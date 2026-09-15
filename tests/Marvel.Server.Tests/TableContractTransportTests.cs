using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
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

        Assert.Equal(16, response.Version);
        Assert.Equal(1, table.GetProperty("active_player").GetInt32());
        Assert.Equal([17, 23], restored.World!.Areas[0].Cards.Select(card => card.Id));
        Assert.Equal([17, 23], restored.World.Relationships.Select(relationship => relationship.Subject));
        Assert.Equal([23, 17], restored.World.Relationships.Select(relationship => relationship.Related));
        Assert.Equal(0, restored.World.Table?.PromptOwner);
        Assert.Equal(1, restored.World.Table?.PublicFocusSeat);
    }

    [Fact]
    public void VersionFifteenCarriesPromptQuestionAndAnchorNamespaceOverTheWire()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Mulligan", "Spider-Man resolves mulligans", false,
            [new Affordance(7, "Choose", 4, 0, "unknown")
                { AnchorKind = AffordanceAnchorKind.Area }])
        {
            DisplayQuestion = "Opening hand",
        };
        var response = new EngineResponse(EngineProtocol.Version, "prompt", "game",
            Capability: null, prompt, Events: []);

        byte[] json = EngineJson.Write(response);
        using JsonDocument document = JsonDocument.Parse(json);
        EngineResponse restored = EngineJson.ReadResponse(json);
        JsonElement encoded = document.RootElement.GetProperty("prompt");

        Assert.Equal(16, response.Version);
        Assert.Equal("Opening hand", encoded.GetProperty("display_question").GetString());
        Assert.Equal((int)AffordanceAnchorKind.Area, encoded.GetProperty("affordances")[0]
            .GetProperty("anchor_kind").GetInt32());
        Assert.Equal("Opening hand", restored.Prompt?.DisplayQuestion);
        Assert.Equal(AffordanceAnchorKind.Area, restored.Prompt?.Affordances[0].AnchorKind);
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
