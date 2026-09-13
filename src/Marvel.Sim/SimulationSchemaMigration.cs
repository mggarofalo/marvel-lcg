using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Rules.Prompts;
using Marvel.Session;

namespace Marvel.Sim;

/// <summary>Reads historic research records without widening their old contracts.</summary>
internal static class SimulationSchemaMigration
{
    internal static DecisionSelector ReadSchemaTwoSelector(JsonElement selector)
    {
        JsonObject root = JsonNode.Parse(selector.GetRawText())?.AsObject()
            ?? throw new JsonException("schema 2 decision selector was null");
        AddHistoricCardAnchorKind(root);
        return root.Deserialize<DecisionSelector>(RecordJson.Options)
            ?? throw new JsonException("schema 2 decision selector was null");
    }

    internal static T ReadSchemaThree<T>(string line)
    {
        JsonObject root = JsonNode.Parse(line)?.AsObject()
            ?? throw new JsonException("schema 3 record was null");
        AddSchemaThreeRecord(root);
        return root.Deserialize<T>(RecordJson.Options)
            ?? throw new JsonException("schema 3 record was null");
    }

    private static void AddSchemaThreeRecord(JsonObject record)
    {
        AddHistoricCardAnchorKind(record["prompt"]?.AsObject());
        AddHistoricCardAnchorKind(record["decision"]?.AsObject());
        foreach (JsonNode? step in record["recent_steps"]?.AsArray() ?? [])
        {
            JsonObject recent = step?.AsObject()
                ?? throw new JsonException("schema 3 recent step was null");
            AddHistoricCardAnchorKind(recent["prompt"]?.AsObject());
            AddHistoricCardAnchorKind(recent["decision"]?.AsObject());
        }
    }

    private static void AddHistoricCardAnchorKind(JsonObject? record)
    {
        if (record is null) return;
        if (record.ContainsKey("anchor_id"))
        {
            AddHistoricCardAnchorKindToSelector(record);
            return;
        }

        AddHistoricCardAnchorKindToAffordances(record);
    }

    private static void AddHistoricCardAnchorKindToSelector(JsonObject selector)
    {
        if (selector.ContainsKey("anchor_kind"))
            throw new JsonException("historic decision selector has anchor_kind");
        selector["anchor_kind"] = selector["decline"]?.GetValue<bool>() == true
            ? null
            : (int)AffordanceAnchorKind.Card;
    }

    private static void AddHistoricCardAnchorKindToAffordances(JsonObject prompt)
    {
        foreach (JsonNode? affordance in prompt["affordances"]?.AsArray() ?? [])
        {
            JsonObject choice = affordance?.AsObject()
                ?? throw new JsonException("historic affordance was null");
            if (choice.ContainsKey("anchor_kind"))
                throw new JsonException("historic affordance has anchor_kind");
            choice["anchor_kind"] = (int)AffordanceAnchorKind.Card;
        }
    }
}
