using System.Text.Json.Nodes;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class SchemaTwoMigrationMeaningTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void VerifiedMeaningSurvivesMigrationWriteReadAndReplayIncludingRedo(int cursor)
    {
        DatasetGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        SessionSave current = RecordedGame(factory);
        Game active = SessionReplay.VerifyAtCursor(current, factory.Compatibility,
            setup => Open(factory, setup), cursor);
        current = current with { Cursor = cursor, CurrentPrompt = PromptRecord.From(active.Pending!) };
        SessionSave legacy = ReadSchemaTwo(current);

        Assert.Contains(Sources(current.Units[1].Decisions[0].Prompt), source => source.AbilityName == "Scientist");
        Assert.All(Sources(legacy.Units[1].Decisions[0].Prompt), source => Assert.Null(source.AbilityName));
        SessionSave migrated = SessionReplay.MigrateSchemaTwo(legacy, factory.Compatibility,
            setup => Open(factory, setup));
        SessionSave restored = SessionSaveJson.Read(SessionSaveJson.Write(migrated));
        Game replayed = SessionReplay.Verify(restored, factory.Compatibility, setup => Open(factory, setup));

        Assert.Equal(SessionReplay.Fingerprint(active), SessionReplay.Fingerprint(replayed));
        Assert.Equal(cursor, restored.Cursor);
        Assert.Equal(2, restored.Units.Count);
        Assert.Equal(SessionSaveJson.Write(current), SessionSaveJson.Write(restored));
    }

    [Fact]
    public void MigrationRejectsChangedRecordedResourcesBeforeAddingMeaning()
    {
        DatasetGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        SessionSave legacy = ReadSchemaTwo(RecordedGame(factory));
        JournalUnit unit = legacy.Units[1];
        JournalStep step = unit.Decisions[0];
        PromptRecord changed = step.Prompt with
        {
            Affordances = [.. step.Prompt.Affordances.Select(offer => offer with
            {
                Costs = [.. offer.Costs.Select(cost => cost with
                {
                    Sources = cost.Sources?.Select(source => source with { Generates = "B" }).ToArray(),
                })],
            })],
        };
        legacy = legacy with { Units = [legacy.Units[0], unit with { Decisions = [step with { Prompt = changed }] }] };

        Assert.Throws<ReplayDivergenceException>(() => SessionReplay.MigrateSchemaTwo(
            legacy, factory.Compatibility, setup => Open(factory, setup)));
    }

    private static SessionSave RecordedGame(DatasetGameFactory factory)
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "migration-meaning",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        Assert.Null(opened.Error);
        EngineResponse kept = Resolve(host, opened, opened.Capability!, Game.ResolveMulligans);
        _ = Resolve(host, kept, opened.Capability!, Game.ChangeForm);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal(2, save.Units.Count);
        return save;
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse response, string capability, string verb)
    {
        Prompt prompt = Assert.IsType<Prompt>(response.Prompt);
        Affordance offer = Assert.Single(prompt.Affordances, item => item.Verb == verb);
        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame(verb, "migration-meaning",
            capability, new EngineDecision(offer.Id, []), response.Revision));
        Assert.Null(resolved.Error);
        return resolved;
    }

    private static ReplayOpenedGame Open(DatasetGameFactory factory, SessionSetup setup)
    {
        OpenedGame opened = factory.Create(new GameSpecification(
            setup.Scenario, setup.Heroes, setup.ModularSets, setup.Seed));
        return new ReplayOpenedGame(opened.Game, opened.SetupEvents);
    }

    private static IEnumerable<ResourceSourceRecord> Sources(PromptRecord prompt) =>
        prompt.Affordances.SelectMany(offer => offer.Costs).SelectMany(cost => cost.Sources ?? []);

    // The frozen schema has computed aliases and no generator display meaning.
    private static SessionSave ReadSchemaTwo(SessionSave save)
    {
        JsonObject json = JsonNode.Parse(SessionSaveJson.Write(save))!.AsObject();
        json["schema"] = 2;
        FreezePrompt(json["current_prompt"]!.AsObject());
        foreach (JsonNode? unit in json["units"]!.AsArray())
            foreach (JsonNode? step in unit!["decisions"]!.AsArray())
            {
                FreezePrompt(step!["prompt"]!.AsObject());
                _ = step["decision"]!["selector"]!.AsObject().Remove("anchor_kind");
            }
        return SessionSaveJson.Read(json.ToJsonString());
    }

    private static void FreezePrompt(JsonObject prompt)
    {
        foreach (JsonNode? item in prompt["affordances"]!.AsArray())
        {
            JsonObject offer = item!.AsObject();
            _ = offer.Remove("anchor_kind");
            if (offer["targets"] is JsonObject targets)
                targets["is_grouped"] = targets["groups"] is JsonArray { Count: > 0 };
            foreach (JsonNode? cost in offer["costs"]!.AsArray()) FreezeCost(cost!.AsObject());
        }
    }

    private static void FreezeCost(JsonObject cost)
    {
        RemoveLaterCostFields(cost);
        cost["has_alternative"] = cost["or_cost"]!.GetValue<string>().Length > 0;
        cost["generators"] = cost["sources"]?.DeepClone() ?? new JsonArray();
        cost["variable_requests"] = cost["variables"]?.DeepClone() ?? new JsonArray();
        cost["resource_costs"] = cost["components"]?.DeepClone() ?? new JsonArray(new JsonObject
        {
            ["cost"] = cost["cost"]!.DeepClone(), ["rule"] = cost["rule"]?.DeepClone(), ["printed"] = false,
        });
    }

    private static void RemoveLaterCostFields(JsonObject cost)
    {
        foreach (JsonNode? source in cost["sources"]?.AsArray() ?? [])
        {
            _ = source!.AsObject().Remove("ability_name");
            _ = source.AsObject().Remove("cost_description");
        }
        foreach (JsonNode? component in cost["components"]?.AsArray() ?? [])
            _ = component!.AsObject().Remove("repeated_resource");
    }
}
