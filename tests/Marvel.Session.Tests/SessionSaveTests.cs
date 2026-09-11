using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
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
    public void SchemaThreeHasAStableStrictTopLevelDocument()
    {
        SessionSave save = Save();

        string json = SessionSaveJson.Write(save);
        SessionSave parsed = SessionSaveJson.Read(json);

        Assert.Equal(json, SessionSaveJson.Write(parsed));
        Assert.StartsWith(
            "{\"format\":\"marvel-session\",\"schema\":3,\"compatibility\":",
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
                "\"schema\":3",
                "\"schema\":4",
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

    [Fact]
    public void SchemaTwoCanBeReadStrictlyButCannotBeWrittenAsCurrent()
    {
        string legacyJson = SessionSaveJson.Write(Save()).Replace(
            "\"schema\":3",
            "\"schema\":2",
            StringComparison.Ordinal);

        SessionSave legacy = SessionSaveJson.Read(legacyJson);

        Assert.Equal(2, legacy.Schema);
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Write(legacy));
    }

    [Fact]
    public void SchemaTwoPromptsUseTheirFrozenShapeBeforeConversion()
    {
        Prompt prompt = new(
            0,
            Question.TurnOption,
            TimingPriority.Untimed,
            string.Empty,
            "Pay",
            false,
            [new Affordance(
                7,
                "Play",
                20,
                0,
                "Pay",
                new TargetRequest([20], 1, 1),
                [new CostOption(20, "1", Sources: [new ResourceSource(8, "M")])])]);
        PromptRecord currentPrompt = PromptRecord.From(prompt);
        JsonObject legacy = Assert.IsType<JsonObject>(JsonSerializer.SerializeToNode(
            Save() with { CurrentPrompt = currentPrompt }, SessionSaveJson.Options));
        legacy["schema"] = 2;
        legacy["current_prompt"] = JournalRecordsTests.SchemaTwoPrompt(currentPrompt);

        SessionSave parsed = SessionSaveJson.Read(legacy.ToJsonString(SessionSaveJson.Options));

        Assert.Equal(2, parsed.Schema);
        Assert.Equal(
            JsonSerializer.Serialize(currentPrompt, JournalJson.Options),
            JsonSerializer.Serialize(parsed.CurrentPrompt, JournalJson.Options));
        legacy["current_prompt"]!["affordances"]![0]!["targets"]!["is_grouped"] = true;
        Assert.Throws<SessionSaveException>(() =>
            SessionSaveJson.Read(legacy.ToJsonString(SessionSaveJson.Options)));
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
