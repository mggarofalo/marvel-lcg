using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.Timing;
using Marvel.Session;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class SessionSaveTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.0-preview.9")]
    [InlineData("1.0.0-preview.10", "1.0.0-preview.2")]
    [InlineData("1.0.0-preview.999999999999999999999", "1.0.0-preview.10")]
    [InlineData("2.0.0", "1.999.999")]
    [InlineData("1.1.0", "1.0.999")]
    public void ApplicationVersionsFollowSemVerPrecedence(string newer, string older)
    {
        Assert.True(ApplicationVersion.Parse(newer).CompareTo(
            ApplicationVersion.Parse(older)) > 0);
        Assert.True(ApplicationVersion.Parse(older).CompareTo(
            ApplicationVersion.Parse(newer)) < 0);
    }

    [Fact]
    public void ApplicationVersionBuildMetadataDoesNotChangePrecedence()
    {
        Assert.Equal(
            0,
            ApplicationVersion.Parse("1.2.3-preview.4+first").CompareTo(
                ApplicationVersion.Parse("1.2.3-preview.4+second")));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("01.0.0")]
    [InlineData("1.0.0-preview.01")]
    [InlineData("1.0.0-")]
    [InlineData("1.0.0+")]
    public void InvalidApplicationVersionsFailClosed(string value)
    {
        Assert.Throws<SessionSaveException>(() => ApplicationVersion.Parse(value));
    }

    [Fact]
    public void CurrentSchemaHasAStableStrictTopLevelDocument()
    {
        SessionSave save = Save();

        string json = SessionSaveJson.Write(save);
        SessionSave parsed = SessionSaveJson.Read(json);

        Assert.Equal(json, SessionSaveJson.Write(parsed));
        Assert.StartsWith(
            "{\"format\":\"marvel-session\",\"schema\":7,\"compatibility\":",
            json,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"revision\":0,\"cursor\":0,\"edit_frontier\":0,"
            + "\"current_prompt\":null,\"units\":[]}",
            json,
            StringComparison.Ordinal);
        Assert.DoesNotContain("capability", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("invitation", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("format")]
    [InlineData("schema")]
    public void UnknownMembersFormatsAndSchemasFailClosed(string change)
    {
        string json = SessionSaveJson.Write(Save());
        string changed = change switch
        {
            "unknown" => "{\"unknown\":true," + json[1..],
            "format" => json.Replace(
                "\"format\":\"marvel-session\"",
                "\"format\":\"future-session\"",
                StringComparison.Ordinal),
            "schema" => json.Replace(
                "\"schema\":7",
                "\"schema\":8",
                StringComparison.Ordinal),
            _ => throw new InvalidOperationException(change),
        };

        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(changed));
    }

    [Fact]
    public void MissingRequiredRecordsAndInvalidHistoryBoundsFailClosed()
    {
        SessionSave save = Save();

        Assert.Throws<SessionSaveException>(() =>
            SessionSaveJson.Read(SessionSaveJson.Write(save).Replace(
                "\"setup\":{",
                "\"removed_setup\":{",
                StringComparison.Ordinal)));
        Assert.Throws<SessionSaveException>(() =>
            SessionSaveJson.Write(save with { Cursor = 1 }));
        Assert.Throws<SessionSaveException>(() =>
            SessionSaveJson.Read(SessionSaveJson.Write(save).Replace(
                "\"revision\":0,",
                string.Empty,
                StringComparison.Ordinal)));
    }

    [Fact]
    public void MissingResourceAllocationMembersFailClosed()
    {
        var decision = new DurableDecision(
            0,
            new DecisionSelector(false, 0, 0, "Action", "Pay", 0),
            [],
            [],
            new Dictionary<string, long>(StringComparer.Ordinal),
            [new ResourceAllocation(0, 0, "M")]);
        string json = JsonSerializer.Serialize(decision, SessionSaveJson.Options);
        string changed = json.Replace("\"cost\":0,", string.Empty, StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DurableDecision>(changed, SessionSaveJson.Options));
    }

    [Theory]
    [InlineData(null)]
    [InlineData('Y')]
    public void ResourceRequirementsRoundTripStrictlyAndPredecessorsCannotClaimThem(char? repeated)
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "Action", "Pay", false,
            [new Affordance(7, "Action", 4, 0, "Pay", Costs:
                [new CostOption(4, "X", Components:
                    [new ResourceCost("X") { RepeatedResource = repeated }])])]);
        SessionSave save = Save() with { CurrentPrompt = PromptRecord.From(prompt) };
        string json = SessionSaveJson.Write(save);
        SessionSave parsed = SessionSaveJson.Read(json);
        Assert.Equal(repeated, parsed.CurrentPrompt!.Affordances[0].Costs![0].Components![0].RepeatedResource);
        Assert.Equal(json, SessionSaveJson.Write(parsed));
        JsonObject legacy = JsonNode.Parse(json)!.AsObject();
        JsonObject component = legacy["current_prompt"]!["affordances"]![0]!["costs"]![0]!["components"]![0]!.AsObject();
        _ = component.Remove("repeated_resource");
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(legacy.ToJsonString()));
        legacy["schema"] = 4;
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(legacy.ToJsonString()));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
    public void OtherSchemasAreRejectedBeforeOpeningAGame(int schema)
    {
        SessionSave save = Save() with { Schema = schema };
        string json = JsonSerializer.Serialize(save, SessionSaveJson.Options);
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(json));
        Assert.Throws<SessionSaveException>(() => SessionReplay.Verify(save, save.Compatibility,
            _ => throw new InvalidOperationException("Unsupported saves must not deal a game")));
    }

    [Fact]
    public void ExplicitDeckOrderRoundTripsAndItsPresenceIsRequired()
    {
        var cards = new List<string> { "01008", "01009", "01008" };
        SessionSave save = Save();
        save = save with { Setup = save.Setup with { PlayerDecks = [new PlayerDeckRecipe(cards)] } };
        cards.Clear();
        string json = SessionSaveJson.Write(save);
        SessionSave restored = SessionSaveJson.Read(json);
        Assert.Equal(["01008", "01009", "01008"], restored.Setup.PlayerDecks![0].Cards);
        Assert.Equal(json, SessionSaveJson.Write(restored));
        JsonObject missing = JsonNode.Parse(json)!.AsObject();
        Assert.True(missing["setup"]!.AsObject().Remove("player_decks"));
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(missing.ToJsonString()));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"cards\":null}")]
    [InlineData("{\"cards\":[null]}")]
    [InlineData("{\"cards\":[\" \"]}")]
    [InlineData("null")]
    public void MalformedDeckRecipesAreReportedAsInvalidSaves(string recipe)
    {
        JsonObject document = JsonNode.Parse(SessionSaveJson.Write(Save()))!.AsObject();
        document["setup"]!["player_decks"] = new JsonArray(JsonNode.Parse(recipe));
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(document.ToJsonString()));
    }

    private static SessionSave Save() => new(
        SessionSave.FormatName,
        SessionSave.CurrentSchema,
        new SessionCompatibility(
            "1.0.0",
            "engine-replay-v1",
            "mt19937-iso-cxx",
            "state-digest-v2",
            new string('a', 64),
            new string('b', 64),
            new string('c', 64)),
        new SessionIdentity(new string('d', 32), "table", "active"),
        new SessionSetup("rhino", ["spider_man"], [], 7),
        new InitialRecord([], 0, "digest"),
        Revision: 0,
        Cursor: 0,
        EditFrontier: 0,
        CurrentPrompt: null,
        Units: []);
}
