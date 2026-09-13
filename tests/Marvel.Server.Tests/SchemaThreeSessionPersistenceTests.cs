using System.Text.Json.Nodes;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class SchemaThreeSessionPersistenceTests
{
    [Fact]
    public void AFilesystemRestartMigratesSchemaThreeJournaledSelectors()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-schema-three-host-{Guid.NewGuid():N}");
        try
        {
            var factory = DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root);
            var first = new EngineHost(
                factory,
                new SchemaThreeCapabilities("schema-three-owner"),
                new Marvel.View.RestrictedVisibilityPolicy(0),
                new FileSessionStore(root));
            EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
                "open",
                "schema-three-table",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 91)));
            Prompt mulligan = Assert.IsType<Prompt>(opened.Prompt);
            Affordance selected = Assert.Single(mulligan.Affordances);
            Assert.Null(first.Exchange(EngineRequest.ResolveGame(
                "keep",
                "schema-three-table",
                opened.Capability!,
                new EngineDecision(selected.Id, []),
                opened.Revision)).Error);

            string directory = Assert.Single(Directory.GetDirectories(root));
            string generation = File.ReadAllText(Path.Combine(directory, "current")).Trim();
            string predecessor = Path.Combine(directory, generation + ".session.json");
            File.WriteAllText(predecessor, SchemaThree(File.ReadAllText(predecessor)));

            var restarted = new EngineHost(
                factory,
                visibility: new Marvel.View.RestrictedVisibilityPolicy(0),
                store: new FileSessionStore(root));
            EngineResponse synced = restarted.Exchange(EngineRequest.SyncGame(
                "sync", "schema-three-table", opened.Capability!));
            StoredSession migrated = Assert.Single(new FileSessionStore(root).Load());
            JournalUnit unit = Assert.Single(migrated.Save.Units);
            JournalStep step = Assert.Single(unit.Decisions);

            Assert.Null(synced.Error);
            Assert.Equal(SessionSave.CurrentSchema, migrated.Save.Schema);
            Assert.Equal("complete", unit.Status);
            Assert.Equal(AffordanceAnchorKind.Card, step.Decision.Selector.AnchorKind);
            Assert.Equal(AffordanceAnchorKind.Card, step.Prompt.Affordances[0].AnchorKind);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static string SchemaThree(string json)
    {
        JsonObject root = Assert.IsType<JsonObject>(JsonNode.Parse(json));
        root["schema"] = 3;
        RemoveAnchorKinds(root["current_prompt"]?.AsObject());
        foreach (JsonNode? unitNode in root["units"]!.AsArray())
        {
            foreach (JsonNode? stepNode in unitNode!["decisions"]!.AsArray())
            {
                JsonObject step = stepNode!.AsObject();
                RemoveAnchorKinds(step["prompt"]!.AsObject());
                _ = step["decision"]!["selector"]!.AsObject().Remove("anchor_kind");
            }
        }

        return root.ToJsonString(SessionSaveJson.Options);
    }

    private static void RemoveAnchorKinds(JsonObject? prompt)
    {
        foreach (JsonNode? affordanceNode in prompt?["affordances"]?.AsArray() ?? [])
            _ = affordanceNode!.AsObject().Remove("anchor_kind");
    }

    private sealed class SchemaThreeCapabilities(string value) : ISessionCapabilityIssuer
    {
        public string Issue() => value;
    }
}
